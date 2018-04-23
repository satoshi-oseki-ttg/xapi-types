using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class VerbType : ObjectGraphType<Verb>
    {
        public VerbType()
        {
            Field<StringGraphType>("id", resolve: context => context.Source.id);
        }
    }
}
