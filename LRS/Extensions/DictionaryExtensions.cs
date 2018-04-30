using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace bracken_lrs.DictionaryExtensions
{
    public static class DictionaryExtensions
    {
        public static void CheckLanguageCodes(this Dictionary<string, string> dictionary)
        {
            foreach (var d in dictionary)
            {
                try
                {
                    new CultureInfo(d.Key);
                }
                catch (ArgumentException)
                {
                    throw new JsonSerializationException($"{d.Key} isn't a valid language code.");
                }
            }
        }       
    }
}
    