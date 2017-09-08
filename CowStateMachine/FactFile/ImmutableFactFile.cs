using System.Collections.Generic;
using System.Dynamic;

namespace CowStateMachine.FactFile
{    public class ImmutableFactFile : AbstractFactFile
    {
        public ImmutableFactFile(string filePath, List<string> header) : base(filePath, header) { }
        private Dictionary<string, List<ExpandoObject>> cowIndex = new Dictionary<string, List<ExpandoObject>>();
        public List<ExpandoObject> getFacts(string IBN)
        {
            if (cowIndex.ContainsKey(IBN))
            {
                return cowIndex[IBN];
            }
            else { return null; }
        }

        public override void addToIndex(ExpandoObject @object)
        {
            if (!cowIndex.ContainsKey(((dynamic)@object).InterbullNumber))
            {
                cowIndex[((dynamic)@object).InterbullNumber] = new List<ExpandoObject>();
            }
            cowIndex[((dynamic)@object).InterbullNumber].Add(@object);
        }
    }
}
