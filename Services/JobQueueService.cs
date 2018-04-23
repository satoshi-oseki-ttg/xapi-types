using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bracken_lrs.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using TinCan;
using TinCan.Documents;
using TinCan.Json;
using RabbitMQ.Client;
using System.Text;
using Hangfire;

namespace bracken_lrs.Services
{
    public class JobQueueService : IJobQueueService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly AppSettings _appSettings;
        private const string dbName = "bracken_lrs"; // This should be per site and each site has collections, states, statements etc?

        private readonly IxApiService _xApiService;

        public JobQueueService(IOptions<AppSettings> optionsAccessor, IxApiService xApiService)
        {
            _appSettings = optionsAccessor.Value;
            
            _client = new MongoClient(_appSettings.MongoDbConnection);
            _db = _client.GetDatabase(dbName);
            _xApiService = xApiService;
        }

        [Queue("statements")]
        public void EnqueueStatement(JObject statement)
        {
            BackgroundJob.Enqueue(() => _xApiService.SaveStatement(statement, null));
        }

        [Queue("states")]
        public void EnqueueState(byte[] value, string stateId, string activityId, string agent)
        {
            BackgroundJob.Enqueue(() => _xApiService.SaveState(value, stateId, activityId, agent));
        }
    }
}
