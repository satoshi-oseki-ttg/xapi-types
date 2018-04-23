using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("Activity")]
    public class Activity : IStatementTarget
    {
        public static readonly string OBJECT_TYPE = "Activity";

        private string objectType;
        public string ObjectType
        {
            get { return OBJECT_TYPE; }
            set { objectType = value; }
        }

        public Uri Id { get; set; }

        public ActivityDefinition Definition { get; set; }
    }
}
