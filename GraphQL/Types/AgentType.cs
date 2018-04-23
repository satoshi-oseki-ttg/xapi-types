using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class AgentType : ObjectGraphType<Agent>
    {
        public AgentType()
        {
            Field(x => x.name, nullable: true);
            Field<AgentAcountType>("account", resolve: context => context.Source.account);
        }
    }
}
