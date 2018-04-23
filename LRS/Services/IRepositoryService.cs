using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using bracken_lrs.Models.xAPI;

namespace bracken_lrs.Services
{
    public interface IRepositoryService
    {
        Task SaveStatement(Statement statement, Guid? statementId, string lrsUrl, string userName);
        Task<Statement> GetStatement(Guid id);
    }
}
