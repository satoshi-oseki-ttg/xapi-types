using bracken_lrs.Model;
using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class ProgressType : ObjectGraphType<ProgressModel>
    {
        public ProgressType()
        {
            Field(x => x.StatementID);
            Field(x => x.UserName);
            Field(x => x.UserRealName);
            Field(x => x.ActivityID);
            Field(x => x.ActivityName);
            Field(x => x.CourseName);
            Field(x => x.Status);
            Field("timestamp", x => x.Timestamp, nullable: true);
        }
    }
}
