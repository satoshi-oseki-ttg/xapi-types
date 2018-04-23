using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    public interface IViewModelCacheService
    {
        string Get(string key);
        void Set(string key, string value);
    }
}
