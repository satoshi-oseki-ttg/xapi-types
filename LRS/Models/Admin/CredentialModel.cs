
using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace bracken_lrs.Models.Admin
{
    public class CredentialModel
    {       
        [BsonId]
        [JsonIgnore]
        public ObjectId _id { get; set; }
        [JsonProperty("id")]
        [BsonElement("id")]
        public Guid Id { get; set; }
        public string Identifier { get; set; }
        public string Password { get; set; }
    }
}
