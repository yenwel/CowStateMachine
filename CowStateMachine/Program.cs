using System;
using System.Dynamic;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Configuration;
using CowStateMachine.Values;
using CowStateMachine.FactFile;

namespace CowStateMachine
{
    class Program
    {
        private static int[] daysinmilk = Enumerable.Range(1, 50).ToArray();
        private static int[] design = daysinmilk;
        private static int maxSlidingWindowSize = 5;
        private static ImmutableDictionary<string,Dictionary<int,CowCycleState>> CowCycleStateHistory; 
        private static List<AbstractFactFile> filecontents = new List<AbstractFactFile>();
        private static string cowEventFile = $@"{Directory.GetCurrentDirectory()}\cowEventFile.csv";
        public static void Main(string[] args)
        {
            if (ConfigurationManager.AppSettings.HasKeys())
            {
                var myMaxSlidingWindowSize = ConfigurationManager.AppSettings["maxSlidingWindowSize"];
                if (myMaxSlidingWindowSize != null)
                    maxSlidingWindowSize = int.Parse(myMaxSlidingWindowSize);
                var myDesign = ConfigurationManager.AppSettings["design"];
                if (myDesign != null)
                    design = myDesign.Split(new char[] { ',' }).Select(x => int.Parse(x)).ToArray();
            }
            readFiles();
            mergeTimeLines();
            writeFile();
        }

        public static void readFiles()
        {
            var localFiles = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\GplusE", "*-with-header.csv");
            foreach (var file in localFiles.Where( x=> !x.Contains("FeedAnalysis-with-header")))
            {
                Console.WriteLine($"Loading {file}");
                filecontents.Add(FactFileBuilder.LoadCSV(file));
            }
        }

        public static void mergeTimeLines()
        {
            var cowCycleStateHistoryBuilder = ImmutableDictionary.CreateBuilder<string, Dictionary<int, CowCycleState>>();
            Parallel.ForEach(filecontents.Where(x => x.FileName == "Animal-with-header").FirstOrDefault().LoadedFacts, (cow) => {
                var cowCycleHistory = buildCowHistory((cow as dynamic));
                cowCycleStateHistoryBuilder.Add((cow as dynamic).InterbullNumber, cowCycleHistory);
            });
            CowCycleStateHistory = cowCycleStateHistoryBuilder.ToImmutable();
            Console.WriteLine("Going backwards in time!");

            var allTimePredicates = filecontents.Where(x => x.Header.Contains("DIM")).Select(x => x.FileName).ToArray();
            foreach (var cowCycleStateHistory in CowCycleStateHistory)
            {
                foreach (var i in daysinmilk.Reverse())
                {
                    var predicates = cowCycleStateHistory.Value[i].events.Keys.ToList();
                    if (i < daysinmilk.Max())
                    {
                        foreach (var pred in allTimePredicates.Except(predicates))
                        {
                            if(cowCycleStateHistory.Value.ContainsKey(i +1) && cowCycleStateHistory.Value[i+1].events.ContainsKey(pred))
                            {
                                if (((cowCycleStateHistory.Value[i + 1].events[pred].DaysInMilk - i) < maxSlidingWindowSize))
                                {
                                    Console.WriteLine($"Going back in Time! Cow {cowCycleStateHistory.Key} on day {i} was missing event in file {pred} taking from the day before.");
                                    cowCycleStateHistory.Value[i].events[pred] = cowCycleStateHistory.Value[i + 1].events[pred];
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Cow {cowCycleStateHistory.Key} had the following predicates on day {daysinmilk.Max()}: {string.Join(" ", predicates)}.");
                    }
                }
            }
        }

        public static void writeFile()
        {
            var delimiter = ";";
            Console.WriteLine("Writing history!");

            var fileList = filecontents.OrderBy(x => x.FileName);
            var headerList = string.Join(";", fileList.Select(x => string.Join(delimiter, x.Header.Select(y => $"{x.FileName.Replace("-with-header", "")}.{y}"))));
            using (var sw = new StreamWriter(cowEventFile))
            {
                sw.WriteLine($"DIM{delimiter}{string.Join(delimiter, headerList)}");
                foreach (var cowCycleStateHistory in CowCycleStateHistory)
                {
                    var cow = cowCycleStateHistory.Key;
                    Console.WriteLine($"Writing history for cow {cow}");
                    foreach (var cowCycleStateDay in cowCycleStateHistory.Value.Where(x=> design.Contains(x.Key)))
                    {
                        var partialCsvs = new List<string>();
                        Console.WriteLine($"Writing history for cow {cow} on day {cowCycleStateDay.Key}");
                        foreach (var file in fileList)
                        {
                            if (cowCycleStateDay.Value.facts.ContainsKey(file.FileName))
                            {
                                partialCsvs.Add(string.Join(delimiter, ((IDictionary<string, object>)cowCycleStateDay.Value.facts[file.FileName].obj).Values));
                            }
                            else if (cowCycleStateDay.Value.events.ContainsKey(file.FileName))
                            {
                                partialCsvs.Add(string.Join(delimiter, ((IDictionary<string, object>)cowCycleStateDay.Value.events[file.FileName].Fact.obj).Values));
                            }
                            else
                            {
                                partialCsvs.Add(string.Join(delimiter, ((IDictionary<string, object>)file.defaultObject).Values));
                            }
                        }
                        sw.WriteLine(cowCycleStateDay.Key + delimiter + string.Join(delimiter.ToString(), partialCsvs));
                    }
                }
            }
            Console.WriteLine("Finished. Please press enter");
            Console.ReadLine();
        }

        private static Dictionary<int, CowCycleState> buildCowHistory(dynamic cow)
        {
            var cowCycleHistory = new Dictionary<int, CowCycleState>();
            foreach (dynamic parity in (filecontents.Where(x => x.FileName == "Calving-with-header").FirstOrDefault() as ImmutableFactFile).getFacts(cow.InterbullNumber))
            {
                Console.WriteLine($"Cow {cow.InterbullNumber} with parity {parity.LactationNumber}.");
                var subject = new CowCycleSubject { interbullNumber = cow.InterbullNumber, parity = int.Parse(parity.LactationNumber) };
                var facts = new Dictionary<string, CowCycleFact>();
                foreach (var filecontent in filecontents.Where(x => x.Header.Contains("InterbullNumber") && !x.Header.Contains("DIM")))
                {
                    if (filecontent.containsInterbullNumber(cow.InterbullNumber))
                    {
                        List<ExpandoObject> localfacts = (filecontent as ImmutableFactFile).getFacts(cow.InterbullNumber);
                        if (localfacts != null && localfacts.Count > 0)
                        {
                            var content = localfacts.First();
                            if (content != null)
                            {
                                Console.WriteLine($"Cow {cow.InterbullNumber} with parity {parity.LactationNumber} found fact in file {filecontent.FileName}.");
                                var myFact =  new CowCycleFact
                                {
                                    subject =
                                    new CowCycleSubject
                                    {
                                        interbullNumber = cow.InterbullNumber,
                                        parity = int.Parse(parity.LactationNumber)
                                    },
                                    pred = filecontent.FileName,
                                    obj = content
                                };
                                facts[filecontent.FileName] = myFact;
                            }
                        }
                    }
                }

                foreach (var day in daysinmilk)
                {
                    var state = new CowCycleState { subject = new CowCycleSubject { interbullNumber = cow.InterbullNumber, parity = int.Parse(parity.LactationNumber) }, dayInMilk = day, facts = facts };
                    var events = new Dictionary<string, CowCycleEvent>();
                    if (day > daysinmilk.Min())
                    {
                        foreach (var @event in cowCycleHistory[day - 1].events)
                        {
                            if((day - @event.Value.DaysInMilk) < maxSlidingWindowSize)
                                events[@event.Key] = @event.Value;
                        }
                    }
                    Console.WriteLine($"Cow {cow.InterbullNumber} with parity {parity.LactationNumber} on day {day}.");
                    foreach (var filecontent in filecontents.Where(x => x.Header.Contains("InterbullNumber") && x.Header.Contains("DIM")))
                    {
                        if (filecontent.containsDIM(day) && filecontent.containsInterbullNumber(cow.InterbullNumber))
                        {                            
                            List<ExpandoObject> localevents = (filecontent as MutableFactFile).getEvents(cow.InterbullNumber, day);
                            if( localevents != null && localevents.Count > 0)
                            {
                               var content = localevents.First();
                                if (content != null)
                                {
                                    Console.WriteLine($"Cow {cow.InterbullNumber} with parity {parity.LactationNumber} on day {day} found event in file {filecontent.FileName}.");
                                    var myevent = new CowCycleEvent
                                    {
                                        Fact = new CowCycleFact
                                        {
                                            subject =
                                            new CowCycleSubject
                                            {
                                                interbullNumber = cow.InterbullNumber,
                                                parity = int.Parse(parity.LactationNumber)
                                            },
                                            pred = filecontent.FileName,
                                            obj = content
                                        },
                                        DaysInMilk = day
                                    };
                                    events[filecontent.FileName] = myevent;
                                }
                            }
                        }                           
                    }
                    state.events = events;
                    cowCycleHistory[day] = state;
                }
            }
            return cowCycleHistory;
        }
    }
}