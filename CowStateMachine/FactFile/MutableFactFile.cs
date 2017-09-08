using System;
using System.Collections.Generic;
using System.Dynamic;

namespace CowStateMachine.FactFile
{
    public class MutableFactFile : AbstractFactFile
    {
        public MutableFactFile(string filePath, List<string> header) : base(filePath, header) { }
        private Dictionary<Tuple<string, int>, List<ExpandoObject>> cowDIMIndex = new Dictionary<Tuple<string, int>, List<ExpandoObject>>();
        public List<ExpandoObject> getEvents(string IBN, int dim)
        {
            var tuple = Tuple.Create(IBN, dim);
            if (cowDIMIndex.ContainsKey(tuple))
            {
                return cowDIMIndex[tuple];
            }
            else { return null; }
        }

        public override void addToIndex(ExpandoObject @object)
        {
            var tuple = Tuple.Create(((dynamic)@object).InterbullNumber, int.Parse(((dynamic)@object).DIM));
            if (!cowDIMIndex.ContainsKey(tuple))
            {
                cowDIMIndex[tuple] = new List<ExpandoObject>();
            }
            cowDIMIndex[tuple].Add(@object);

        }
    }
}
