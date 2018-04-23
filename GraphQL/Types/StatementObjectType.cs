using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class StatementObjectType : ObjectGraphType<StatementObject>
    {
        public StatementObjectType()
        {
            Field(x => x.id);
            Field<ActivityDefinitionType>("definition", resolve: context => context.Source.definition);
        }
    }
}
