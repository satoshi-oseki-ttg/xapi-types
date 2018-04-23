using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ServiceStack.Redis;
using TinCan;

namespace bracken_lrs.Services
{
    public class ViewModelCacheService : IViewModelCacheService
    {
        private readonly RedisManagerPool _manager = new RedisManagerPool("localhost:6379");

        public string Get(string key)
        {
            using (var client = _manager.GetClient())
            {
                return client.Get<string>(key);
            }
        }
        public void Set(string key, string value)
        {
            using (var client = _manager.GetClient())
            {
                client.Set(key, value);
            }
        }
    }
}
