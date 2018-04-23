using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    public interface IVmLastStatementsService
    {
        int Limit { get; set; }
        void Process(Statement statement);
    }
}
