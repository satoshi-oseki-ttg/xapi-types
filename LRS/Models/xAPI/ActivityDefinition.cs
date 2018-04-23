using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public class ActivityDefinition
    {
        public Uri Type { get; set; }
        public Uri MoreInfo { get; set; }

        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public Dictionary<string, object> Extensions { get; set; }

        // SO: Added the fields below
        public string InteractionType;
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
