
using System;
using System.Net.Mail;
using bracken_lrs.Models.xAPI;
using bracken_lrs.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.Json
{
    public class AgentGroupConverter : JsonConverter
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

                try
                {
                    return GetAgentOrGroup(jobj, type);
                }
                catch (Exception e)
                {
                    throw new JsonSerializationException(e.ToString());
                }
            }
        }

        private Agent GetAgentOrGroup(JObject jObject, string type)
        {
            Agent result = null;

            if (type == Group.OBJECT_TYPE)
            {
                result = jObject.ToObject<Group>();
            }
            else if (type == Agent.OBJECT_TYPE)
            {
                result = jObject.ToObject<Agent>();
            }

            if (result == null)
            {
                throw new JsonSerializationException($"Type {type} is not valid.");
            }

            return result;
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
