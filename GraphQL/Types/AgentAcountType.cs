using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class AgentAcountType : ObjectGraphType<AgentAccount>
    {
        public AgentAcountType()
        {
            Field(x => x.name, nullable: true);
        }
    }
}
