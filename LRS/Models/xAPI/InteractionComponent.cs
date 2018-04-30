using System;
using System.Collections.Generic;
using bracken_lrs.DictionaryExtensions;

namespace bracken_lrs.Models.xAPI
{
    public class InteractionComponent
    {
        public string Id { get; set; }
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
    }
}
