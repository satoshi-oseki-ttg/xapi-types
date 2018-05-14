using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("Group")]
    public class Group : Agent
    {
        public static readonly new string OBJECT_TYPE = "Group";
        public override string ObjectType { get; set; }
        public List<Agent> Member { get; set; }
    }
}
