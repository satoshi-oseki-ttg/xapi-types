using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    public interface IxApiService
    {
        void SaveStatement(JObject statement, Guid? statementId = null);
        void SaveState(byte[] value, string stateId, string activityId, string agent);
        string GetState(string stateId, string activityId, string agent);
        Statement[] GetStatements();
        Statement GetStatement(Guid id);
        Agent[] GetActors();
        StatementObject[] GetCourseStatements();
        StatementObject[] GetCourseStatements(string username);
        Agent[] GetActorsInCourse(string courseName);
        Statement[] GetCourseUserStatements(string courseName, string username);
    }
}
