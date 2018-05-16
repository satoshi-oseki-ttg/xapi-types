using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI
{
    // Object returned from a GET agents request
    public class Person
    {
        public static readonly string OBJECT_TYPE = "Person";
        public string ObjectType { get { return OBJECT_TYPE; } }
        public IList<string> Name { get; set; }
        public IList<string> Mbox { get; set; }
        [JsonProperty("mbox_sha1sum")]
        public IList<string> MboxSha1Sum { get; set; }
        [JsonProperty("openid")]
        public IList<string> OpenId { get; set; }
        public IList<AgentAccount> Account { get; set; }

        public Person()
        {
            Name = new List<string>();
            Mbox = new List<string>();
            MboxSha1Sum = new List<string>();
            OpenId = new List<string>();
            Account = new List<AgentAccount>();
        }
    }
}
