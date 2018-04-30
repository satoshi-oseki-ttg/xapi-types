using System;

namespace bracken_lrs.Models.xAPI.Documents
{
    public class StateDocument : Document
    {
        public Activity Activity { get; set; }
        public Agent Agent { get; set; }
        public Guid? Registration { get; set; }
    }
}
