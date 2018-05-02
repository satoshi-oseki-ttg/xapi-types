using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using bracken_lrs.DictionaryExtensions;
using bracken_lrs.Models.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public class ActivityDefinition
    {
        private List<string> validInteractionTypes = new List<string>
        {
            "true-false", "choice", "fill-in", "long-fill-in", "matching",
            "performance", "sequencing", "likert", "numeric", "other"
        };
        public Uri Type { get; set; }
        public Uri MoreInfo { get; set; }
        private Dictionary<string, string> name;
        public Dictionary<string, string> Name
        {
            get { return name; }
            set
            {
                value.CheckLanguageCodes();

                name = value;
            }
        }
        private Dictionary<string, string> description;
        public Dictionary<string, string> Description
        {
            get { return description; }
            set
            {
                value.CheckLanguageCodes();

                description = value;
            }
        }
        [JsonConverter(typeof(ExtensionConverter))]
        public BsonDocument Extensions { get; set; }
        private string interactionType;
        public string InteractionType
        {
            get { return interactionType; }
            set
            {
                if (!validInteractionTypes.Contains(value))
                {
                    throw new JsonSerializationException($"{value} isn't a valid interaction type.");
                }
                interactionType = value;
            }
        }
        public List<string> CorrectResponsesPattern { get; set; }
        public List<InteractionComponent> Choices { get; set; }
        [JsonProperty("fill-in")]
        public List<InteractionComponent> FillIn { get; set; }        
        public List<InteractionComponent> Scale { get; set; }
        [JsonProperty("long-fill-in")]
        public List<InteractionComponent> LongFillIn { get; set; }        
        public List<InteractionComponent> Source { get; set; }
        public List<InteractionComponent> Target { get; set; }
        public List<InteractionComponent> Numeric { get; set; }
        public List<InteractionComponent> Other { get; set; }
        public List<InteractionComponent> Performance { get; set; }
        public List<InteractionComponent> Sequencing { get; set; }
        [JsonProperty("true-false")]
        public List<InteractionComponent> TrueFalse { get; set; }
        public List<InteractionComponent> Steps { get; set; }

        // Validation
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Type != null)
            {
                try
                {
                    new Uri(Type.ToString());
                }
                catch (Exception)
                {
                    throw new Exception("Activity type isn't a valid IRI.");
                }
            }

            if (MoreInfo != null)
            {
                try
                {
                    new Uri(MoreInfo.ToString());
                }
                catch (Exception)
                {
                    throw new Exception("Activity moreInfo isn't a valid IRI.");
                }
            }

            if (CorrectResponsesPattern != null
                || Choices != null
                || FillIn != null
                || Scale != null
                || LongFillIn != null
                || Source != null
                || Target != null
                || Numeric != null
                || Other != null
                || Performance != null
                || Sequencing != null
                || TrueFalse != null
                || Steps != null)
            {
                if (InteractionType == null)
                {
                    throw new Exception("InteractionType isn't set when interaction component is expected.");
                }
            }
        }
    }
}
