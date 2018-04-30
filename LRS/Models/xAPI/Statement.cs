using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace bracken_lrs.Models.xAPI
{
    public class Statement : StatementBase
    {
        [BsonId]
        public ObjectId _id { get; set; }
        // [JsonProperty("id", NamingStrategyType = typeof(DefaultNamingStrategy), NamingStrategyParameters = new object[] {true})]
        [JsonProperty("id")]
        [BsonElement("id")]
        public Guid Id { get; set; }
        public DateTime Stored { get; set; }
        public Agent Authority { get; set; }
        // public TCAPIVersion version { get; set; } // TODO: include this
    }
}
