using System.Collections.Generic;

namespace bracken_lrs.Models.xAPI
{
    public class ContextActivities
    {
        public Activity Parent { get; set; }
        public Activity Grouping { get; set; }
        public Activity Category { get; set; }
        public Activity Other { get; set; }
    }
}
