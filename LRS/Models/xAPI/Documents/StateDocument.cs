using System;
using bracken_lrs.Models.Json;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI.Documents
{
    public class StateDocument : Document
    {
        public Activity Activity { get; set; }
        [JsonConverter(typeof(AgentGroupConverter))]
        public Agent Agent { get; set; }
        public Guid? Registration { get; set; }
    }
}
