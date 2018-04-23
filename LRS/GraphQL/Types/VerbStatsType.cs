using bracken_lrs.Model;
using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class VerbStatsType : ObjectGraphType<VerbStatsModel>
    {
        public VerbStatsType()
        {
            Field(x => x.Id);
            Field(x => x.Count);
        }
    }
}
