using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class ActivityDefinitionType : ObjectGraphType<ActivityDefinition>
    {
        public ActivityDefinitionType()
        {
            Field<StringGraphType>("name", resolve: context => context.Source.name.Map["und"]);
            Field<LanguageMapType>("map", resolve: context => context.Source.name);
        }
    }
}
