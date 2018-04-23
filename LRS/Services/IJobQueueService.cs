using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TinCan;

namespace bracken_lrs.Services
{
    public interface IJobQueueService
    {
        void EnqueueStatement(JObject statement);
        void EnqueueState(byte[] value, string stateId, string activityId, string agent);
    }
}
