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
using bracken_lrs.Models.xAPI;
using MongoDB.Bson.Serialization.Conventions;

namespace bracken_lrs.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly AppSettings _appSettings;
        private const string dbName = "lrs_dev"; // This should be per site and each site has collections, states, statements etc?

        public RepositoryService(IOptions<AppSettings> optionsAccessor)
        {
            _appSettings = optionsAccessor.Value;
            
            MongoDefaults.GuidRepresentation = MongoDB.Bson.GuidRepresentation.Standard;

            var pack = new ConventionPack();
            pack.Add(new CamelCaseElementNameConvention());
            ConventionRegistry.Register("Use camel case", pack, t => true);
            pack.Add(new IgnoreIfNullConvention(true));
            ConventionRegistry.Register("Ignore fields with null", pack, t => true);

            BsonClassMap.RegisterClassMap<Activity>();
            BsonClassMap.RegisterClassMap<Agent>();
            BsonClassMap.RegisterClassMap<Group>();
            BsonClassMap.RegisterClassMap<StatementRef>();
            BsonClassMap.RegisterClassMap<SubStatement>();

            _client = new MongoClient(_appSettings.MongoDbConnection);

            _db = _client.GetDatabase(dbName);
         }
        
        public async Task SaveStatement(Statement statement, Guid? statementId, string lrsUrl, string userName)
        {
            if (statement == null)
            {
                return;
            }

            if (statementId == null && statement.Id == null)
            {
                statement.Id = Guid.NewGuid();
            }

            statement.Stored = DateTime.UtcNow;
            if (statement.Timestamp == null)
            {
                statement.Timestamp = statement.Stored;
            }

            statement.Authority = new Agent
            {
                Account = new AgentAccount(new Uri(lrsUrl), userName)
            };

            await _db.GetCollection<Statement>("statements")
                .InsertOneAsync(statement);
        }

        public async Task<Statement> GetStatement(Guid id)
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }

            var cursor = await collection.FindAsync(x => x.Id == id);
            var statements = cursor.ToList();

            return (statements.Count > 0) ? statements[0] : null;
        }
    }
}
