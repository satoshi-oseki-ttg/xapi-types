using System;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public abstract class StatementBase
    {
        private const String ISODateTimeFormat = "o";
        [JsonProperty(Required = Required.Always)]
        public Agent Actor { get; set; }
        public Verb Verb { get; set; }
        [BsonElement("object")] // store as "object" instead of "target"
        [JsonProperty("object")]
        [JsonConverter(typeof(StatementTargetConverter))]
        public IStatementTarget Target { get; set; }
        public Result Result { get; set; }
        public Context Context { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
