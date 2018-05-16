using System;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using bracken_lrs.Models.Json;

namespace bracken_lrs.Models.xAPI
{
    [BsonDiscriminator("Agent")]
    public class Agent : IStatementTarget, IEquatable<Agent>
    {
        public static readonly string OBJECT_TYPE = "Agent";
        public virtual string ObjectType { get; set; }
        [JsonConverter(typeof(StrictNumberToStringConverter))]
        public string Name { get; set; }
        public string Mbox { get; set; }
        [JsonProperty("mbox_sha1sum")]
        public string MboxSha1Sum { get; set; }
        [JsonProperty("openid")]
        public string OpenId { get; set; }
        public AgentAccount Account { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Mbox)
                || !string.IsNullOrEmpty(MboxSha1Sum)
                || !string.IsNullOrEmpty(OpenId)
                || !string.IsNullOrEmpty(Account?.Name) && Account?.HomePage != null;
        }
        
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
