
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using bracken_lrs.JsonExtensions;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.Json
{
    // The 'extensions' field can have any object structure.
    // To accomodate this dynamic object, the 'BsonDocument' is used.
    // In addition, a fullstop character isn't allowed for a property name in MongoDB.
    // Fullstop characters are replaced with an HTML code '&46;' when saved and back to '.' when read.
    public class ExtensionConverter : JsonConverter
    {
        private readonly JsonSerializer defaultSerializer = new JsonSerializer();
        private const string htmlCodeForFullstop = "&46;";
        private const string fullstop = ".";

        public override bool CanConvert(Type objectType) 
        {
            return objectType.IsClass;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            var docs = BsonDocument.Parse(jo.ToString().Replace(fullstop, htmlCodeForFullstop));

            return docs;
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bsonDocument = value as BsonDocument;
            var json = bsonDocument.ToString().Replace(htmlCodeForFullstop, fullstop);

            writer.WriteRawValue(json);
        }
    }
}
