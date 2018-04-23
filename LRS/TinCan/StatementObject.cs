using System;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace TinCan
{
    public class StatementObject : StatementTarget // For field 'object' in Statement
    {
        public JObject ToJObject(TCAPIVersion version)
        {
            return null;
        }

        [BsonElement("id")]
        public string id;
        public Uri Id
        {
            get {
                try
                {
                    return new Uri(id);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            set { this.id = value.ToString(); }
        }
        public string objectType;
        public string ObjectType
        {
            get; set;
        }

        public ActivityDefinition definition;
    }
}
