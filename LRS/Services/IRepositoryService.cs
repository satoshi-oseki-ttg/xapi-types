using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using bracken_lrs.Models.xAPI;
using System.Collections.Generic;
using System.IO;
using Microsoft.Net.Http.Headers;

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
        Task SaveState(byte[] value, string stateId, string activityId, Agent agent);
        Task<string> GetState(string stateId, string activityId, Agent agent);
    }
}
