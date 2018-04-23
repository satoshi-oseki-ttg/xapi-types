using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("Group")]
    public class Group : Agent
    {
        public static readonly new string OBJECT_TYPE = "Group";
        public new/*??override*/ string ObjectType
        {
            get { return OBJECT_TYPE; }
            set { objectType = value; } }

        public List<Agent> member { get; set; }
    }
}
