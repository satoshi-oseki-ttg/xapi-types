using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using bracken_lrs.Models.xAPI;
using System.Collections.Generic;

namespace bracken_lrs.Services
{
    public interface IRepositoryService
    {
        Task<string[]> SaveStatement(JObject json, Guid? statementId, string lrsUrl, string userName);
        Task<Statement> GetStatement(Guid? id, bool toGetVoided = false);
        Task<IList<Statement>> GetStatements();
        Task SaveState(byte[] value, string stateId, string activityId, Agent agent);
        Task<string> GetState(string stateId, string activityId, Agent agent);

    }
}
