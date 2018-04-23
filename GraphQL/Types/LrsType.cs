using bracken_lrs.GraphQL.Models;
using GraphQL.Types;

namespace bracken_lrs.GraphQL.Types
{
    class LrsType : ObjectGraphType<LrsModel>
    {
        public LrsType()
        {
            Field<ListGraphType<StatementType>>("statements", resolve: context => context.Source.Statements);
        }
    }
}
