using System;
using System.Collections;
using System.Collections.Generic;
using bracken_lrs.DictionaryExtensions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Models.xAPI
{
    public class Attachment
    {
        public Uri UsageType { get; set; }
        private Dictionary<string, string> display;
        public Dictionary<string, string> Display
        {
            get { return display; }
            set
            {
                value.CheckLanguageCodes();

                display = value;
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
        public string ContentType { get; set; }
        public long Length { get; set; }
        public string Sha2 { get; set; }
    }
}
