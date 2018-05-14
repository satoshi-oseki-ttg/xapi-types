using System;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{ 
    [BsonDiscriminator("StatementRef")]
    public class StatementRef : IStatementTarget
    {
        public static readonly String OBJECT_TYPE = "StatementRef";
        public string ObjectType { get; set; }
        public StatementRef()
        {
        }
        public Guid? Id { get; set; }

        public StatementRef(Guid id)
        {
            Id = id;
        }

        // Validation
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Id == null || Id == Guid.Empty)
            {
                throw new Exception("A Sub-Statement must have its id set.");
            }
        }
    }
}
