using System;
using System.Collections.Generic;
using bracken_lrs.Models.Json;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public abstract class StatementBase
    {
        private const String ISODateTimeFormat = "o";
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(AgentGroupConverter))]
        public Agent Actor { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Verb Verb { get; set; }
        [BsonElement("object")] // store as "object" instead of "target"
        [JsonProperty("object", Required = Required.Always)]
        [JsonConverter(typeof(StatementTargetConverter))]
        public IStatementTarget Target { get; set; }
        public Result Result { get; set; }
        public Context Context { get; set; }
        public DateTime? Timestamp { get; set; }
        public List<Attachment> Attachments { get; set; }
    }
}
