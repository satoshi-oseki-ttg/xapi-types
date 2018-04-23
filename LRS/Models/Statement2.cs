using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Statement2
{
    [BsonId]
    public ObjectId _id { get; set; }
    [BsonElement("id")]
    public Guid id { get; set; }
    public string Verb { get; set; }
    public DateTime Stored { get; set; }
    public string Actor { get; set; }
}
