using System;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("Agent")]
    public class Agent : IStatementTarget, IEquatable<Agent>
    {
        public static readonly string OBJECT_TYPE = "Agent";
        protected string objectType; // SO: Added this field for deserialisation
        public virtual string ObjectType
        {
            get { return OBJECT_TYPE; }
            set { objectType = value; }
        }

        public string Name { get; set; }
        public string Mbox { get; set; }
        public string MboxSha1Sum { get; set; }
        public string Openid { get; set; }
        public AgentAccount Account { get; set; }

        public bool Equals(Agent other)
        {
            return Account != null && Account != null &&
                Account.Name == other.Account.Name;
        }

        public override int GetHashCode()
        {
            return this.Account.GetHashCode(); 
        }
    }
}
