
using System;
using bracken_lrs.JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.Json
{
    public class StrictNumberToStringConverter : JsonConverter
    {
        private readonly JsonSerializer defaultSerializer = new JsonSerializer();

        public override bool CanConvert(Type objectType) 
        {
            return objectType.IsNumberType();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return defaultSerializer.Deserialize(reader, objectType);
                default:
                    throw new JsonSerializationException($"Token \"{reader.Value}\" of type {reader.TokenType} isn't a JSON string");
            }
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
