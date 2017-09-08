using System.Dynamic;

namespace CowStateMachine.Values
{    public struct CowCycleFact
    {
        public CowCycleSubject subject { get; set; }
        public string pred { get; set; }
        public ExpandoObject obj { get; set; }
    }
}
