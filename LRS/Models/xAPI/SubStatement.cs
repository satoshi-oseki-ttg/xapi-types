using System;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("SubStatement")]
    public class SubStatement : StatementBase, IStatementTarget
    {
        public static readonly String OBJECT_TYPE = "SubStatement";
        public string ObjectType { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {

        }

        // Validation
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Target.ObjectType == OBJECT_TYPE) // Nested sub-statement not allowed
            {
                throw new Exception("A Sub-Statement cannot have a Sub-Statement");
            }
        }
    }
}
