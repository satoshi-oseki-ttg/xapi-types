
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public class StatementTargetConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Not implemented yet");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            } 
            else
            {
                var jobj = JObject.Load(reader);
                
                if (jobj["objectType"] == null)
                {
                    return null;
                }
   
                var type = (string)jobj["objectType"];
                return GetStatementTarget(jobj, type);
            }
        }

        private IStatementTarget GetStatementTarget(JObject jObject, string type)
        {
            IStatementTarget target = null;

            if (type == Group.OBJECT_TYPE)
            {
                target = jObject.ToObject<Group>();
            }
            else if (type == Agent.OBJECT_TYPE)
            {
                target = jObject.ToObject<Agent>();
            }
            else if (type == Activity.OBJECT_TYPE)
            {
                target = jObject.ToObject<Activity>();
            }
            else if (type == StatementRef.OBJECT_TYPE)
            {
                target = jObject.ToObject<StatementRef>();
            }

            return target;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return false;
        }
    }
}
