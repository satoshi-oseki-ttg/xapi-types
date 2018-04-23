using System;
using System.Collections.Generic;
using System.Xml;

namespace bracken_lrs.Models.xAPI
{
    public class Result
    {
        public bool? Completion { get; set; }
        public bool? Success { get; set; }
        public string Response { get; set; }
        public string Duration { get; set; } // 4.6 ISO 8601 Durations (https://github.com/adlnet/xAPI-Spec/blob/master/xAPI-Data.md#durations)
        public Score Score { get; set; }
        public Dictionary<string, object> Extensions { get; set; }
    }
}
