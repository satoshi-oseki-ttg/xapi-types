using System;
using bracken_lrs.Models.Json;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI
{
    public class Score
    {
        [JsonConverter(typeof(StrictStringToNumberConverter))]
        public double? Scaled { get; set; }
        [JsonConverter(typeof(StrictStringToNumberConverter))]
        public double? Raw { get; set; }
        [JsonConverter(typeof(StrictStringToNumberConverter))]
        public double? Min { get; set; }
        [JsonConverter(typeof(StrictStringToNumberConverter))]
        public double? Max { get; set; }
    }
}
