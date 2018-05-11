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
        Task<Statement> GetStatement(Guid? id, bool toGetVoided = false, IList<StringWithQualityHeaderValue> acceptLanguages = null, bool isCanonical = false);
        StatementsResult GetStatements(Uri verbId, IList<StringWithQualityHeaderValue> acceptLanguages, bool isCanonical);
        StatementsResult GetStatements(int limit, DateTime since, IList<StringWithQualityHeaderValue> acceptLanguages, bool isCanonical);
        Task SaveState(byte[] value, string stateId, string activityId, Agent agent);
        Task<string> GetState(string stateId, string activityId, Agent agent);

    }
}
