using System;
using bracken_lrs.Models.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace bracken_lrs.Models.xAPI
{
    public class Statement : StatementBase
    {
        [BsonId]
        [JsonIgnore]
        public ObjectId _id { get; set; }
        // [JsonProperty("id", NamingStrategyType = typeof(DefaultNamingStrategy), NamingStrategyParameters = new object[] {true})]
        [JsonProperty("id")]
        [BsonElement("id")]
        public Guid Id { get; set; }
        public DateTime Stored { get; set; }
        [JsonConverter(typeof(AgentGroupConverter))]
        public Agent Authority { get; set; }
        // public TCAPIVersion version { get; set; } // TODO: include this
    }
}
