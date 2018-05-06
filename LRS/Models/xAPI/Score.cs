using System;
using System.Runtime.Serialization;
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

        // Validation
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // This can't be checked in JSON schema at the moment.
            // This might be supported with '$data' property in the future.
            if (Raw != null)
            {
                if (Min != null && Raw < Min || Max != null && Raw > Max)
                {
                    throw new Exception("Raw must be between Min and Max");
                }
            }
        }
    }
}
