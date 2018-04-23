using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    public interface IViewModelService
    {
        void Update(JObject statement);
    }
}
