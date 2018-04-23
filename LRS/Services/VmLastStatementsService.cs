using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bracken_lrs.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    // Manage a list of users (actors)
    public class VmLastStatementsService : IVmLastStatementsService
    {
        private readonly IViewModelCacheService _viewModelCacheService;

        public int Limit { get; set; }

        public VmLastStatementsService(IViewModelCacheService viewModelCacheService)
        {
            _viewModelCacheService = viewModelCacheService;
        }

        public void Process(Statement statement)
        {
            var json = _viewModelCacheService.Get("last_statements");
            if (json == null || json == "nil")
            {
                var list = new List<ProgressModel>();
                list.Add(GetProgress(statement));
                _viewModelCacheService.Set("last_statements", JsonConvert.SerializeObject(list));                
            }
            else
            {
                var lastStatements = JsonConvert.DeserializeObject<IList<ProgressModel>>(json);
                lastStatements.Add(GetProgress(statement));
                if (lastStatements.Count > Limit)
                {
                    lastStatements.RemoveAt(0);
                }
                _viewModelCacheService.Set("last_statements", JsonConvert.SerializeObject(lastStatements));
            }
        }

        private ProgressModel GetProgress(Statement statement)
        {
            var userName = statement.actor.account.name;
            var userRealName = statement.actor.name;
            var timestamp = statement.timestamp?.ToString("dd/MM/yyyy H:mm:ss");
            var verb = statement.verb.id.Substring(statement.verb.id.LastIndexOf("/") + 1);
            var target = ((Activity)statement.target)?.definition.name.Map["und"];
            var entry = new ProgressModel
            {
                UserName = userName,
                UserRealName = userRealName,
                ActivityName = target as string,
                Status = verb,
                Timestamp = statement.timestamp
            };

            return entry;
        }
    }
}
