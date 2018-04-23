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
    public class VmProgressService : IVmProgressService
    {
        private readonly IViewModelCacheService _viewModelCacheService;

        public VmProgressService(IViewModelCacheService viewModelCacheService)
        {
            _viewModelCacheService = viewModelCacheService;
        }

        public void Process(Statement statement)
        {
            var progressJson = _viewModelCacheService.Get("progress");
            var progress = (progressJson == null || progressJson == "nil") ?
                new List<ProgressModel>() :
                JsonConvert.DeserializeObject<IList<ProgressModel>>(progressJson);

            if (Analyse(statement, progress))
            {
                _viewModelCacheService.Set("progress", JsonConvert.SerializeObject(progress));
            }
        }

        private bool Analyse(Statement statement, IList<ProgressModel> list)
        {
            var changes = true;
            var verb = statement.verb.id.Substring(statement.verb.id.LastIndexOf("/") + 1);
            var userName = statement.actor.account.name;
            var userRealName = statement.actor.name;
            var activityID = statement.context.contextActivities.parent?[0].Id;
            var progressEntry = FindUserCourseEntry(list, userName, activityID);
            switch (verb)
            {
                case "attempted":
                    if (progressEntry != null) // restarted
                    {
                        progressEntry.Status = "restarted";
                        progressEntry.Timestamp = DateTime.UtcNow;
                    }
                    else
                    {
                        list.Add(new ProgressModel
                        {
                            StatementID = statement.id.ToString(),
                            UserName = userName,
                            ActivityID = activityID,
                            UserRealName = userRealName,
                            CourseName = ((Activity)(statement.target)).definition.name.Map["und"] as string,
                            Status = "started",
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    break;
                case "completed":
                case "passed":
                case "failed":
                    if (progressEntry != null)
                    {
                        progressEntry.Status = verb;
                        progressEntry.Timestamp = DateTime.UtcNow;
                    }
                    break;
                default:
                    changes = false;
                    break;
            }

            return changes;
        }

        private ProgressModel FindUserCourseEntry(IList<ProgressModel> list, string userName, string activityID)
        {
            return list.FirstOrDefault(x => x.UserName == userName && x.ActivityID == activityID);
        }
    }
}
