using GraphQL.Types;
using TinCan;

namespace bracken_lrs.GraphQL.Types
{
    public class LanguageMapType : ObjectGraphType<LanguageMap>
    {
        public LanguageMapType()
        {
            Field("name", x => x.Map["und"].ToString(), nullable: true);
        }
    }
}
