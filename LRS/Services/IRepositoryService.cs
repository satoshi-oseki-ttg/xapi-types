using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using bracken_lrs.Models.xAPI;
using System.Collections.Generic;
using System.IO;
using Microsoft.Net.Http.Headers;
using bracken_lrs.Models.xAPI.Documents;

namespace bracken_lrs.Services
{
    public interface IRepositoryService
    {
        Task<string[]> SaveStatements(object json, Guid? statementId, string lrsUrl, string userName);
        Task<string[]> SaveStatement(Statement statement, Guid? statementId, string lrsUrl, string userName);
        Task<Statement> GetStatement(Guid? id, bool toGetVoided = false, IList<StringWithQualityHeaderValue> acceptLanguages = null, string format = "exact");
        StatementsResult GetStatements
        (
            Agent agent,
            Uri verbId,
            Uri activity,
            Guid registration,
            int limit,
            DateTime since,
            DateTime until,
            IList<StringWithQualityHeaderValue> acceptLanguages,
            string format,
            bool ascending
        );
        Task SaveState(byte[] value, string stateId, string activityId, Agent agent, string contentType);
        Task<string> GetState(string stateId, string activityId, Agent agent);
        Task<StateDocument> GetStateDocument(string stateId, string activityId, Agent agent);
        Task<bool> DeleteStateDocument(string stateId, string activityId, Agent agent);
        Task SaveActivityProfile(byte[] value, string activityId, string profileId, string contentType);
        Task<ActivityProfileDocument> GetActivityProfileDocument(string activityId, string profileId);
        Task<IList<ActivityProfileDocument>> GetActivityProfileDocuments(string activityId, DateTime? since);
        Task<bool> DeleteActivityProfile(string activityId, string profileId);
        Task SaveAgentProfile(byte[] value, Agent agent, string profileId, string contentType);
        Task<AgentProfileDocument> GetAgentProfileDocument(Agent agent, string profileId);
        Task<IList<AgentProfileDocument>> GetAgentProfileDocuments(Agent agent, DateTime? since);
        Task<bool> DeleteAgentProfile(Agent agent, string profileId);
        Task<Activity> GetActivity(string activityId);
    }
}
