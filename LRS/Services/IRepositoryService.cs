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
        Task<string[]> SaveStatementsAsync(object json, Guid? statementId, string lrsUrl, string userName);
        Task<string[]> SaveStatementAsync(Statement statement, Guid? statementId, string lrsUrl, string userName);
        Task<Statement> GetStatementAsync(Guid? id, bool toGetVoided = false, IList<StringWithQualityHeaderValue> acceptLanguages = null, string format = "exact");
        Task<StatementsResult> GetStatementsAsync
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
        Task SaveStateAsync(byte[] value, string stateId, string activityId, Agent agent, Guid? registration, string contentType);
        Task<string> GetStateAsync(string stateId, string activityId, Agent agent);
        Task<StateDocument> GetStateDocumentAsync(string stateId, string activityId, Agent agent, Guid? registration);
        Task<IList<StateDocument>> GetStateDocumentsAsync(string activityId, Agent agent, Guid? registration, DateTime? since = null);
        Task<bool> DeleteStateDocumentAsync(string stateId, string activityId, Agent agent, Guid? registration);
        Task SaveActivityProfileAsync(byte[] value, string activityId, string profileId, string contentType);
        Task<ActivityProfileDocument> GetActivityProfileDocumentAsync(string activityId, string profileId);
        Task<IList<ActivityProfileDocument>> GetActivityProfileDocumentsAsync(string activityId, DateTime? since = null);
        Task<bool> DeleteActivityProfileAsync(string activityId, string profileId);
        Task SaveAgentProfileAsync(byte[] value, Agent agent, string profileId, string contentType);
        Task<AgentProfileDocument> GetAgentProfileDocumentAsync(Agent agent, string profileId);
        Task<IList<AgentProfileDocument>> GetAgentProfileDocumentsAsync(Agent agent, DateTime? since);
        Task<bool> DeleteAgentProfileAsync(Agent agent, string profileId);
        Task<Activity> GetActivityAsync(string activityId);
        Task<Person> GetPersonAsync(Agent agent);
    }
}
