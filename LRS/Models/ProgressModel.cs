using System;
using TinCan;

namespace bracken_lrs.Model
{
    public class ProgressModel
    {
        public string StatementID { get; set; }
        public string UserName { get; set; }
        public string UserRealName { get; set; }
        public string ActivityID { get; set; }
        public string ActivityName { get; set; }
        public string CourseName { get; set; }
        public string Status { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
