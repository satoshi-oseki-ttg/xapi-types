using System;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("SubStatement")]
    public class SubStatement : StatementBase, IStatementTarget
    {
        public static readonly String OBJECT_TYPE = "SubStatement";
        public String ObjectType { get { return OBJECT_TYPE; } }
    }
}
