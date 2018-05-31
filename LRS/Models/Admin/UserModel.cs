using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace bracken_lrs.Models.Admin
{
    public class UserModel
    {
        [BsonId]
        [JsonIgnore]
        public ObjectId _id { get; set; }
        [JsonProperty("id")]
        [BsonElement("id")]
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public IList<Guid> Tenants { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
    }
}
