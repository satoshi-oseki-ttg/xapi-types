using System;
using Extensions = System.Collections.Generic.Dictionary<string, object>;

namespace bracken_lrs.Models.xAPI
{
    public class Context
    {
        public Guid? Registration { get; set; }
        public Agent Instructor { get; set; }
        public Agent Team { get; set; }
        public ContextActivities ContextActivities { get; set; }
        public string Revision { get; set; }
        public string Platform { get; set; }
        public string Language { get; set; }
        public StatementRef Statement { get; set; }
        public Extensions Extensions { get; set; }
    }
}
