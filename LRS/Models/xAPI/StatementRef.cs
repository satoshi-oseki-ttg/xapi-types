using System;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{ 
    [BsonDiscriminator("StatementRef")]
    public class StatementRef : IStatementTarget
    {
        public static readonly String OBJECT_TYPE = "StatementRef";
        private string objectType;
        public string ObjectType
        {
            get { return OBJECT_TYPE; }
            set { objectType = value; }
        }

        public Guid? Id { get; set; }

        public StatementRef(Guid id)
        {
            Id = id;
        }
    }
}
