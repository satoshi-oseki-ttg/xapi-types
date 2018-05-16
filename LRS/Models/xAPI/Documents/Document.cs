using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI.Documents
{
    public abstract class Document
    {
        // [BsonIgnoreIfDefault]
        // public ObjectId Id { get; set; }
        // public String id { get; set; }
        [BsonId]
        [BsonIgnoreIfDefault] // generate id if not set
        [JsonIgnore]
        public ObjectId _id { get; set; }
        [BsonElement("id")]
        public string Id { get; set; }
        public String Etag { get; set; }
        public DateTime Timestamp { get; set; }
        public String ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}
