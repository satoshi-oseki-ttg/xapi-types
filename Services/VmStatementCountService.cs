using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    // Updates statements' count view model
    public class VmStatementCountService : IViewModelCreationService
    {
        private readonly IViewModelCacheService _viewModelCacheService;

        public VmStatementCountService(IViewModelCacheService viewModelCacheService)
        {
            _viewModelCacheService = viewModelCacheService;
        }

        public void Process(Statement statement)
        {
            var numStatements = _viewModelCacheService.Get("num_statements");
            if (numStatements == null || numStatements == "nil")
            {
                _viewModelCacheService.Set("num_statements", "1");                
            }
            else
            {
                var count = Int64.Parse(numStatements);
                _viewModelCacheService.Set("num_statements", (count+1).ToString());
            }
        }
    }
}
