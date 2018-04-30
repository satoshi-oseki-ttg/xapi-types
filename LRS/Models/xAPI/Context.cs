using System;
using System.Collections.Generic;
using System.Globalization;
using bracken_lrs.Models.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI
{
    public class Context
    {
        public Guid? Registration { get; set; }
        public Agent Instructor { get; set; }
        public Agent Team { get; set; }
        public ContextActivities ContextActivities { get; set; }
        public string Revision { get; set; }
        public string Platform { get; set; }
        private string language;
        public string Language
        {
            get { return language; }
            set
            {
                try
                {
                    new CultureInfo(value);
                }
                catch (ArgumentException)
                {
                    throw new JsonSerializationException($"{value} isn't a valid language code.");
                }

                language = value;
            }
        }
        public StatementRef Statement { get; set; }
        [JsonConverter(typeof(ExtensionConverter))]
        public BsonDocument Extensions { get; set; }
    }
}
