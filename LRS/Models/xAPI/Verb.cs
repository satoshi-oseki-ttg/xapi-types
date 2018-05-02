using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using bracken_lrs.DictionaryExtensions;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI
{
    public class Verb
    {
        public Uri Id { get; set; }
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

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (Id == null)
            {
                throw new Exception("Verb must have its id.");
            }

            try
            {
                new Uri(Id.ToString());
            }
            catch (Exception)
            {
                throw new Exception("Verb id must be valid IRI.");
            }
        }
    }
}
