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

            if (await IsTargetVoided(statement))
            {
                throw new Exception("Target has been voided.");
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

            await _db.GetCollection<Statement>(statementCollection)
                .InsertOneAsync(statement);
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

            using (var statementRefCurosr = await collection.FindAsync(x => x.Target as StatementRef != null))
            {
                using (var voidedCursor = await collection.FindAsync(x => ((StatementRef)(x.Target)).Id == id))
                {
                    var voided = voidedCursor.ToList();
                    return voided.Count() > 0;
                }
            }           
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