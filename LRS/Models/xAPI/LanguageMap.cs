using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace bracken_lrs.Models.xAPI
{
    public class LanguageMap
    {
        private Dictionary<string, object> map;
        [BsonExtraElements]
        public Dictionary<string, object> Map { get { return map; }}

        public LanguageMap() {
            map = new Dictionary<string, object>();
        }
        
        public LanguageMap(Dictionary<string, object> map)
        {
            this.map = map;
        }

        public Boolean isEmpty()
        {
            return map.Count > 0 ? false : true;
        }

        public void Add(string lang, string value)
        {
            this.map.Add(lang, value);
        }
    }
}
