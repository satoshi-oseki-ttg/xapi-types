using bracken_lrs.Services;
using Newtonsoft.Json.Serialization;

namespace bracken_lrs.Models.Json
{
    public class xApiValidationResolver : DefaultContractResolver
    {
        public IxApiValidationService XApiValidationService { get; private set;}

        public xApiValidationResolver(IxApiValidationService xApiValidationService)
        {
            XApiValidationService = xApiValidationService;
        }
    }
}
