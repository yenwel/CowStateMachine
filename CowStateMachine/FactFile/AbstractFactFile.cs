using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace CowStateMachine.FactFile
{
    public abstract class AbstractFactFile
    {
        protected static char[] delimiters = { ';' };
        protected List<ExpandoObject> _loadedFacts;
        protected string _filePath;
        protected string _fileName;
        protected List<string> _header = new List<string>();
        protected List<string> _headerAllowed = new List<string>();
        protected HashSet<string> _interbullNumbers = new HashSet<string>();
        protected HashSet<int> _DIMs = new HashSet<int>();
        private static string[] forbidden = new[] { "CountryCode", "CowNumber", "ExtraCowId", "CalvingDate", "DietTreatment", "PercentHolstein", "CalvingEase", "NumberOfCalves" };
        protected ExpandoObject _def;

        protected AbstractFactFile(string filePath, List<string> header)
        {
            _filePath = filePath;
            _header = header;
            _fileName = Path.GetFileNameWithoutExtension(filePath);
            foreach (var field in _header)
            {
                if (_fileName == "Animal-with-header" || (_fileName == "Calving-with-header" && (!forbidden.Contains(field))) || (!forbidden.Contains(field) && field != "LactationNumber"))
                    _headerAllowed.Add(field);
            }
            _def = new ExpandoObject();
            foreach (var head in _headerAllowed)
            {
                ((IDictionary<string, object>)_def).Add(new KeyValuePair<string, object>(head, ""));
            }
            _loadedFacts = ReadFileData(filePath, _header);
        }

        public List<ExpandoObject> LoadedFacts { private set { } get { return _loadedFacts; } }
        public string FileName { private set { } get { return _fileName; } }
        public List<string> Header { private set { } get { return _headerAllowed; } }
        public bool containsInterbullNumber(string IBN) { return _interbullNumbers.Contains(IBN); }
        public bool containsDIM(int dim) { return _DIMs.Contains(dim); }


        public ExpandoObject defaultObject { private set { } get { return _def; } }

        /// <summary>
        /// Reads the data file of CSV.
        /// Skips the first line, as that has the header row.
        /// Very simple file read and parse the CSV.
        /// </summary>
        /// <param name="fileName">File of CSV to be read</param>
        /// <param name="Fields">The Header Row parsed to identify the columns</param>
        /// <returns>List of ExpandoObject, one object for each line in the CSV file</returns>
        private List<ExpandoObject> ReadFileData(string fileName, List<string> Fields)
        {
            List<ExpandoObject> result = new List<ExpandoObject>();
            using (StreamReader rdr = new StreamReader(fileName))
            {
                string line = rdr.ReadLine();
                while (!rdr.EndOfStream)
                {
                    string IBN = "";
                    int dim = 0;
                    line = rdr.ReadLine();
                    var tokens = line.Split(delimiters);
                    dynamic add = new ExpandoObject();
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        if (_headerAllowed.Contains(Fields[i]))
                        {
                            ((IDictionary<string, object>)add).Add(new KeyValuePair<string, object>(Fields[i], tokens[i]));
                            if (Fields[i] == "InterbullNumber")
                            {
                                _interbullNumbers.Add(tokens[i]);
                                IBN = tokens[i];

                            }
                            else if (Fields[i] == "DIM")
                            {
                                _DIMs.Add(int.Parse(tokens[i]));
                                dim = int.Parse(tokens[i]);
                            }
                        }
                    }
                    addToIndex(add);
                    result.Add(add);
                }
            }
            return result;
        }

        public virtual void addToIndex(ExpandoObject @object)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Reads the header line from a CSV file, and use that line as the field names for reading that file.
        /// </summary>
        /// <param name="fileName">Filename of the CSV to be read</param>
        /// <returns>List of strings, one string for each column in the header record</returns>
        public static List<string> ReadHeader(string fileName)
        {
            string fieldList;
            using (StreamReader rdr = new StreamReader(fileName))
            {
                fieldList = rdr.ReadLine();
            }
            return fieldList.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Select(A => A.Trim()).ToList();
        }
    }
}
