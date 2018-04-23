using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;


namespace bracken_lrs.Models.xAPI
{
    public class ExtensionsX
    {
        //?? private Dictionary<Uri, JToken> map;
        //[BsonExtraElements]
        public Dictionary<string, object> map;
        //?? private Dictionary<string, JToken> map;
        //public Dictionary<Uri, JToken> Map { get { return map; }}

        public ExtensionsX()
        {
            map = new Dictionary<string, object>();
            //?? map = new Dictionary<Uri, JToken>();
        }

        public Boolean isEmpty()
        {
            return map.Count > 0 ? false : true;
        }
    }
}