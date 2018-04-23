using System.Collections.Generic;

namespace bracken_lrs.Models.xAPI
{
    public class ContextActivities
    {
        public IList<Activity> Parent { get; set; }
        public IList<Activity> Grouping { get; set; }
        public IList<Activity> Category { get; set; }
        public IList<Activity> Other { get; set; }
    }
}
