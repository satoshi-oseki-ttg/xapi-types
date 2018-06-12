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

            // 2.4.4 Object
            // Objects which are provided as a value for this property SHOULD include an "objectType" property.
            // If not specified, the objectType is assumed to be Activity.
            if (type == null)
            {
                type = Activity.OBJECT_TYPE;
            }

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
        //jObject["id"] = jObject["id"].ToString().Replace("http:///", "http://");
        jObject = CleanUpUrls(jObject);
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
    private JObject CleanUpUrls(JObject jObject)
    {
      string temp = jObject["id"].ToString();
      if (!string.IsNullOrEmpty(temp))
      {
        // Fix urls with 'http:///'
        string working = temp.Replace("http:///", "http://");

        // Fix urls that only have 'http://'
        if(working.StartsWith("http://") && working.Length == 7)
        {
          working = "http://empty";
        }

        jObject["id"] = working;
        
      }
        return jObject;
    }


    }
}
