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
using MongoDB.Driver.Linq;
using Newtonsoft.Json.Linq;
using bracken_lrs.Models.xAPI;
using MongoDB.Bson.Serialization.Conventions;
using bracken_lrs.Models.xAPI.Documents;
using Newtonsoft.Json;
using bracken_lrs.Models.Json;

namespace bracken_lrs.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly AppSettings _appSettings;
        private const string dbName = "lrs_dev"; // This should be per site and each site has collections, states, statements etc?
        private const string statementCollection = "statements";
        private const string stateCollection = "states";
        private readonly IxApiValidationService _xApiValidationService;

        public RepositoryService(IOptions<AppSettings> optionsAccessor, IxApiValidationService xApiValidationService)
        {
            _appSettings = optionsAccessor.Value;
            _xApiValidationService = xApiValidationService;
            
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
        
        public async Task<string[]> SaveStatement(JObject jObject, Guid? statementId, string lrsUrl, string userName)
        {
            _xApiValidationService.ValidateStatement(jObject);

            var statement = JsonConvert.DeserializeObject<Statement>(jObject.ToString());

            if (statementId == null && (statement.Id == null || statement.Id == Guid.Empty))
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

            await _db.GetCollection<Statement>(statementCollection)
                .InsertOneAsync(statement);
            
            return await Task.FromResult(new [] { statement.Id.ToString() });
        }

        private void ValidateStatement(Statement statement)
        {
            if (statement.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided")
                && statement.Target as StatementRef == null)
            {
                throw new Exception("StatementRef isn't set for verb 'voided'.");
            }

            _xApiValidationService.ValidateVerb(statement.Verb);
        }

        public async Task<Statement> GetStatement(Guid? id, bool toGetVoided = false)
        {
            if (!toGetVoided && await IsVoided(id))
            {
                return null;
            }

            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return null;
            }
            var cursor = await collection.FindAsync(x => x.Id == id);
            var statements = cursor.ToList();

            return (statements.Count > 0) ? statements[0] : null;
        }

        private async Task<bool> IsVoided(Guid? id)
        {
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return false;
            }

            using (var statementsCursor = await collection.FindAsync(new BsonDocument()))
            {
                var statements = statementsCursor.ToList();
                foreach (var s in statements)
                {
                    if (s.Target as StatementRef != null
                        && ((StatementRef)s.Target).Id == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<IList<Statement>> GetStatements()
        {
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return null;
            }
            var cursor = await collection.FindAsync(new BsonDocument());
            var statements = cursor.ToList();

            return statements;
        }

        private async Task<bool> IsTargetVoided(Statement statement)
        {
            var activity = statement.Target as Activity;
            if (activity == null)
            {
                return false;
            }
            var targetId = activity.Id;

            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return false;
            }

            using (var voidedCursor = await collection.FindAsync(x => x.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided")))
            {
                var voided = voidedCursor.ToList();
                foreach (var s in voided)
                {
                    var statementRef = s.Target as StatementRef;
                    if (statementRef == null)
                    {
                        continue;
                    }
                    var voidedStatement = await GetStatement(statementRef.Id, true);
                    if (voidedStatement == null)
                    {
                        continue;
                    }
                    var voidedTarget = voidedStatement.Target as Activity;
                    if (voidedTarget != null && voidedTarget.Id == targetId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task SaveState(byte[] value, string stateId, string activityId, Agent agent)
        {
            if (value == null)
            {
                return;
            }

            var state = new StateDocument
            {
                Id = stateId,
                Activity = new Activity
                {
                    Id = new Uri(activityId)
                },
                Agent = agent,
                Content = value
            };

            var collection = _db.GetCollection<StateDocument>(stateCollection);

            await collection.FindOneAndReplaceAsync<StateDocument>
            (
                x =>
                    x.Id == stateId &&
                    x.Activity.Id == new Uri(activityId) &&
                    x.Agent.Account.Name == state.Agent.Account.Name,
                state,
                new FindOneAndReplaceOptions<StateDocument>
                {
                    IsUpsert = true
                }
            );
        }

        public async Task<string> GetState(string stateId, string activityId, Agent agent)
        {
            var collection = _db.GetCollection<StateDocument>(stateCollection);
            if (collection == null)
            {
                return null;
            }

            var cursor = await collection.FindAsync
                (
                    x =>
                        x.Id == stateId &&
                        x.Activity.Id == new Uri(activityId) &&
                        x.Agent.Account.Name == agent.Account.Name
                );
            var state = await cursor.FirstOrDefaultAsync();

            if (state == null)
            {
                return string.Empty;
            }
            
            var contentBytes = state.Content;
            var stateAsString = contentBytes != null ? System.Text.Encoding.UTF8.GetString(contentBytes) : null;
            
            return stateAsString;
        }
    }
}
