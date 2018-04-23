using GraphQL.Types;
using Newtonsoft.Json;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class StatementType : ObjectGraphType<Statement>
    {
        public StatementType()
        {
            Field<StringGraphType>("id", resolve: context => context.Source.id.ToString());
            Field<VerbType>("verb", resolve: context => context.Source.verb);
            Field(x => x.timestamp, nullable: true);
            Field<AgentType>("actor", resolve: context => context.Source.actor);
            Field<StatementObjectType>("object", resolve: context => context.Source.statementObject);
            Field("json", x => JsonConvert.SerializeObject(x));
        }
    }
}