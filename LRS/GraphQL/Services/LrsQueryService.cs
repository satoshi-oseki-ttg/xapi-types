using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bracken_lrs.GraphQL.Models;
using bracken_lrs.Model;
using bracken_lrs.Services;
using Newtonsoft.Json;
using TinCan;

namespace bracken_lrs.GraphQL.Services
{
    public class LrsQueryService : ILrsQueryService
    {
        private readonly IxApiService _xApiService;
        private readonly IViewModelCacheService _viewModelCacheService;

        public LrsQueryService(IxApiService xApiService, IViewModelCacheService viewModelCacheService)
        {
            _xApiService = xApiService;
            _viewModelCacheService = viewModelCacheService;
        }

        public LrsModel GetLrsModel()
        {
            return new LrsModel
            {
                Statements = _xApiService.GetStatements()
            };
        }

        public IList<Statement> GetAllStatements()
        {
            return null;
        }

        public string GetNumberOfStatements()
        {
            var count =  _viewModelCacheService.Get("num_statements");
            if (count == null || count == "nil")
            {
                return "0";
            }
            else
            {
                return count;
            }
        }

        public Statement GetStatement(Guid id)
        {
            return _xApiService.GetStatement(id);
        }

        public IList<ProgressModel> GetLastStatements()
        {
            var lastStatements = _viewModelCacheService.Get("last_statements");
            if (lastStatements == null || lastStatements == "nil")
            {
                return new List<ProgressModel>();
            }
            else
            {
                return JsonConvert.DeserializeObject<IList<ProgressModel>>(lastStatements);
            }
        }

        public IList<VerbStatsModel> GetVerbStats()
        {
            var verbStats = _viewModelCacheService.Get("verb_stats");
            if (verbStats == null || verbStats == "nil")
            {
                return new List<VerbStatsModel>();
            }
            else
            {
                return JsonConvert.DeserializeObject<IList<VerbStatsModel>>(verbStats);
            }
        }

        public IList<ProgressModel> GetProgress()
        {
            var progress = _viewModelCacheService.Get("progress");
            if (progress == null || progress == "nil")
            {
                return new List<ProgressModel>();
            }
            else
            {
                return JsonConvert.DeserializeObject<IList<ProgressModel>>(progress);
            }
        }

        public IList<Agent> GetActors()
        {
            return JsonConvert.DeserializeObject<Agent[]>(_viewModelCacheService.Get("users"));
            // return _xApiService.GetActors(); // _viewModelCacheService.Get("users"); // it retrieves actors using key 'actors'
        }

        public IList<Agent> GetActorsInCourse(string courseName)
        {
            return _xApiService.GetActorsInCourse(courseName);
        }

        public IList<StatementObject> GetCourses()
        {
            return _xApiService.GetCourseStatements();
        }

        public IList<StatementObject> GetCourses(string username)
        {
            return _xApiService.GetCourseStatements(username);
        }

        public IList<Statement> GetCourseUserStatements(string courseName, string username)
        {
            return _xApiService.GetCourseUserStatements(courseName, username);
        }
    }
}
