using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    // Manage a list of users (actors)
    public class UserViewModelCreationService : IViewModelCreationService
    {
        private readonly IViewModelCacheService _viewModelCacheService;

        public UserViewModelCreationService(IViewModelCacheService viewModelCacheService)
        {
            _viewModelCacheService = viewModelCacheService;
        }

        public void Process(Statement statement)
        {
            var users = _viewModelCacheService.Get("users");
            var userList = users == "nil" ? new List<Agent>() : JsonConvert.DeserializeObject<IList<Agent>>(users);
            var saved = userList.FirstOrDefault(x => x.account.name == statement.actor.account.name);
            if (saved == null)
            {
                userList.Add(statement.actor);
                var actorJson = JsonConvert.SerializeObject(userList);
                _viewModelCacheService.Set("users", actorJson);
            }
        }
    }
}
