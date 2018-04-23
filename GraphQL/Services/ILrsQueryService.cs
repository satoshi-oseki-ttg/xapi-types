using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bracken_lrs.GraphQL.Models;
using bracken_lrs.Model;
using TinCan;

namespace bracken_lrs.GraphQL.Services
{
    public interface ILrsQueryService
    {
        LrsModel GetLrsModel();
        IList<Statement> GetAllStatements();
        string GetNumberOfStatements();
        Statement GetStatement(Guid id);
        IList<ProgressModel> GetLastStatements();
        IList<VerbStatsModel> GetVerbStats();
        IList<ProgressModel> GetProgress();
        IList<Agent> GetActors();
        IList<Agent> GetActorsInCourse(string courseName);
        IList<StatementObject> GetCourses();
        IList<StatementObject> GetCourses(string username);
        IList<Statement> GetCourseUserStatements(string courseName, string username);
    }
}
