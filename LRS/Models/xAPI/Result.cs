using System;
using System.Collections.Generic;
using System.Xml;
using bracken_lrs.Models.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public class Result
    {
        [JsonProperty("completion")]
        [JsonConverter(typeof(StrictStringToBoolConverter))]
        public bool? Completion { get; set; }
        [JsonConverter(typeof(StrictStringToBoolConverter))]
        public bool? Success { get; set; }
        public string Response { get; set; }
        public string Duration { get; set; } // 4.6 ISO 8601 Durations (https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#durations)
        public Score Score { get; set; }
        [JsonConverter(typeof(ExtensionConverter))]
        public BsonDocument Extensions { get; set; }
    }
}
