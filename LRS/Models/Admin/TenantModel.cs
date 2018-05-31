using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace bracken_lrs.Models.Admin
{
    public class TenantModel
    {
        [BsonId]
        [JsonIgnore]
        public ObjectId _id { get; set; }
        [JsonProperty("id")]
        [BsonElement("id")]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IList<CredentialModel> LrsCredentials { get; set; }
        public IList<Guid> Users { get; set; }
    }
}
