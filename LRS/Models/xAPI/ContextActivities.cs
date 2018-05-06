using System.Collections.Generic;
using bracken_lrs.Models.Json;
using Newtonsoft.Json;

namespace bracken_lrs.Models.xAPI
{
    public class ContextActivities
    {
        [JsonConverter(typeof(SingleOrArrayConverter<Activity>))]
        public IList<Activity> Parent { get; set; }
        [JsonConverter(typeof(SingleOrArrayConverter<Activity>))]
        public IList<Activity> Grouping { get; set; }
        [JsonConverter(typeof(SingleOrArrayConverter<Activity>))]
        public IList<Activity> Category { get; set; }
        [JsonConverter(typeof(SingleOrArrayConverter<Activity>))]
        public IList<Activity> Other { get; set; }
    }
}
