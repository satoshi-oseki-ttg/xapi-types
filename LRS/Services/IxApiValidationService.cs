using System;
using System.Threading.Tasks;
using bracken_lrs.Models.xAPI;
using Newtonsoft.Json.Linq;

namespace bracken_lrs.Services
{
    public interface IxApiValidationService
    {
        void ValidateStatement(JObject statement);
        void ValidateAgent(Agent agent);
        void ValidateGroup(Group group);
        void ValidateVerb(Verb verb);
    }
}
