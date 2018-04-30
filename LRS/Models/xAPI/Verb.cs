using System;
using System.Collections.Generic;
using System.Globalization;
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
    }
}
