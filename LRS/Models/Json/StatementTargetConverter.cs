using System;
using System.Net.Mail;
using bracken_lrs.Models.xAPI;
using bracken_lrs.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.Json
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
                    throw new Exception("Statement target must specify its objectType.");
                }
   
                var type = (string)jobj["objectType"];

                try
                {
                    return GetStatementTarget(jobj, type);
                }
                catch (Exception e)
                {
                    throw new JsonSerializationException(e.ToString());
                }
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
            else if (type == SubStatement.OBJECT_TYPE)
            {                
                target = jObject.ToObject<SubStatement>();
            }

            if (target == null)
            {
                throw new JsonSerializationException($"Type {type} is not valid.");
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
