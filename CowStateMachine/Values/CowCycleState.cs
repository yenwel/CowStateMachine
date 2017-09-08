using System.Collections.Generic;

namespace CowStateMachine.Values
{    public struct CowCycleState
    {
        public CowCycleSubject subject { get; set; }
        public int dayInMilk { get; set; }
        public Dictionary<string, CowCycleEvent> events { get; set; }
        public Dictionary<string, CowCycleFact> facts { get; set; }
    }
}
