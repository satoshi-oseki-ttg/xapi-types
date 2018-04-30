using System;
using System.Collections;
using System.Collections.Generic;
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
        public string[] CorrectResponsesPattern;
        //public InteractionType interactionType { get; set; }
        //public List<String> correctResponsesPattern { get; set; }
        public List<InteractionComponent> Choices { get; set; }
        public List<InteractionComponent> Scale { get; set; }
        public List<InteractionComponent> Source { get; set; }
        public List<InteractionComponent> Target { get; set; }
        public List<InteractionComponent> Steps { get; set; }
    }
}
