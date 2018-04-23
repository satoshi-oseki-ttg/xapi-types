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
    // Updates statements' count view model
    public class VmVerbStatsService : IVmVerbStatsService
    {
        private readonly IViewModelCacheService _viewModelCacheService;

        public VmVerbStatsService(IViewModelCacheService viewModelCacheService)
        {
            _viewModelCacheService = viewModelCacheService;
        }

        public void Process(Statement statement)
        {
            var verbStatsJson = _viewModelCacheService.Get("verb_stats"); // Json [{ value: <string>, count: <number> }]
            if (verbStatsJson == null || verbStatsJson == "nil")
            {
                var verbStats = new List<VerbStatsModel>();
                verbStats.Add(new VerbStatsModel { Id = statement.verb.id, Count = 1 });
                _viewModelCacheService.Set("verb_stats", JsonConvert.SerializeObject(verbStats));                
            }
            else
            {
                var verbStats = JsonConvert.DeserializeObject<IList<VerbStatsModel>>(verbStatsJson);
                var verb = statement.verb.id;
                var found = verbStats.FirstOrDefault(x => x.Id == verb);
                if (found != null)
                {
                    found.Count++;
                }
                else
                {
                    verbStats.Add(new VerbStatsModel { Id = verb, Count = 1 });
                }
                _viewModelCacheService.Set("verb_stats", JsonConvert.SerializeObject(verbStats));
            }
        }
    }
}
