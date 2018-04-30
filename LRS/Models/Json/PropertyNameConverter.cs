
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using bracken_lrs.JsonExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.Json
{
    public class PropertyNameConverter : JsonConverter
    {
        private readonly JsonSerializer defaultSerializer = new JsonSerializer();
        private readonly List<string> _validProperties = new List<string> {
            "id", "actor", "objectType", "name", "mbox",
            "account", "homePage", "mbox_sha1sum", "openid",
            "verb", "display",
            "definition", "type", "description", "moreInfo", "extensions",
            "object",
            "context", "registration", "revision", "platform", "language", "statement",
            "instructor", "team", "contextActivities", "parent", "category", "other", "grouping",
            "interactionType", "correctResponsesPattern", "scale",
            "choices", "source", "target", "steps", 
            "result", "score", "scaled", "raw", "min", "max",
            "success", "completion", "response", "duration",
            "version", "timestamp", "authority", "member",
            "attachments", "usageType", "contentType", "length", "sha2", "fileUrl"
        };

        private readonly List<string> _languageMapProperties = new List<string> {
            "display", "definition.name", "description"
        };

        private bool IsLanguageMapProperty(string path)
        {
            foreach (var lp in _languageMapProperties)
            {
                if (path.Contains(lp))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool CanConvert(Type objectType) 
        {
            return objectType.IsClass;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            WalkNode(jObject, null, prop =>
            {
                if (!IsLanguageMapProperty(prop.Parent.Path)
                    && !prop.Parent.Path.Contains("extensions")
                    && !_validProperties.Contains(prop.Name))
                {
                    throw new JsonSerializationException($"{prop.Name} isn't a valid property name.");
                }
                if (!prop.Parent.Path.Contains("extensions") && prop.Value.Type == JTokenType.Null)
                {
                    throw new JsonSerializationException($"{prop.Name} can't be set to null.");                    
                }
            });

            // foreach (var jProperty in jObject.Properties())
            // {
            //     if (!_validProperties.Contains(jProperty.Name))
            //     {
            //         throw new JsonSerializationException($"{jProperty.Name} isn't a valid property name.");
            //     }
            // }

            // The 'reader' has reached to the end of JSON at this poiint.
            // JsonReader is forward-only, so a new reader is required.
            var resetReader = jObject.CreateReader();
            return defaultSerializer.Deserialize(resetReader, objectType);
        }
        private void WalkNode
        (
            JToken node,
            Action<JObject> objectAction = null,
            Action<JProperty> propertyAction = null
        )
        {
            if (node.Type == JTokenType.Object)
            {
                if (objectAction != null) objectAction((JObject) node);

                foreach (JProperty child in node.Children<JProperty>())
                {
                    if (propertyAction != null) 
                    {
                        propertyAction(child);
                    }
                    WalkNode(child.Value, objectAction, propertyAction);
                }
            }
            else if (node.Type == JTokenType.Array)
            {
                foreach (JToken child in node.Children())
                {
                    WalkNode(child, objectAction, propertyAction);
                }
            }
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
