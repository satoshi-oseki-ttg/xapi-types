using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bracken_lrs.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
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
using System.IO;
using System.Net.Http;
using bracken_lrs.DictionaryExtensions;
using System.Text;
using bracken_lrs.Models.Admin;

namespace bracken_lrs.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        private readonly Uri voidedVerb = new Uri("http://adlnet.gov/expapi/verbs/voided");
        private readonly IMongoClient _client;
        public IMongoClient Client { get { return _client; } }
        private IMongoDatabase _db;
        public IMongoDatabase Db { set { _db = value; } }
        private readonly AppSettings _appSettings;
        private const string dbName = "dev"; // This should be per site and each site has collections, states, statements etc?
        private const string statementCollection = "statements";
        private const string stateCollection = "states";
        private const string activityProfileCollection = "activities";
        private const string agentProfileCollection = "agents";
        private const string userCollection = "users";
        private const string tenantCollection = "tenants";
        private readonly IxApiValidationService _xApiValidationService;
        private readonly IMultipartStatementService _multipartStatementService;

        public RepositoryService(
            IOptions<AppSettings> optionsAccessor,
            IxApiValidationService xApiValidationService,
            IMultipartStatementService multipartStatementService
        )
        {
            _appSettings = optionsAccessor.Value;
            _xApiValidationService = xApiValidationService;
            _multipartStatementService = multipartStatementService;
            
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

        public async Task<string[]> SaveStatementsAsync(object obj, Guid? statementId, string lrsUrl, string userName)
        {
            JArray jObjects = new JArray();
            if (obj as JObject != null)
            {
                jObjects.Add(obj);
            }
            if (obj as JArray != null)
            {
                jObjects = obj as JArray;
            }

            var ids = new List<string>();

            foreach (var jObject in jObjects) // Validate all of the statements, then save if all valid
            {
                _xApiValidationService.ValidateStatement(jObject as JObject);
                var statement = JsonConvert.DeserializeObject<Statement>(jObject.ToString());
                await ValidateStatementAsync(statement);
            }

            foreach (var jObject in jObjects)
            {
                var id = await SaveStatementAsync(jObject as JObject, statementId, lrsUrl, userName);
                ids.Add(id);
            }

            return ids.ToArray();
        }

        public async Task<string[]> SaveStatementAsync(Statement statement, Guid? statementId, string lrsUrl, string userName)
        {
            return new [] { await DoSaveStatementAsync(statement, statementId, lrsUrl, userName) };
        }

        private async Task<string> SaveStatementAsync(JObject jObject, Guid? statementId, string lrsUrl, string userName)
        {
            var statement = JsonConvert.DeserializeObject<Statement>(jObject.ToString());

            return await DoSaveStatementAsync(statement, statementId, lrsUrl, userName);
        }

        private async Task<string> DoSaveStatementAsync(Statement statement, Guid? statementId, string lrsUrl, string userName)
        {
            if (statement.Id == null || statement.Id == Guid.Empty)
            {
                statement.Id = (statementId == null)
                    ? Guid.NewGuid()
                    : statementId.GetValueOrDefault();
            }

            if (statement.Stored != null)
            {
                statement.Stored = DateTime.UtcNow;
            }

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
            
            return await Task.FromResult(statement.Id.ToString());
        }

        private async Task ValidateStatementAsync(Statement statement)
        {
            if (statement.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided") // this is done in StatementBase model
                && statement.Target as StatementRef == null)
            {
                throw new Exception("StatementRef isn't set for verb 'voided'.");
            }

            if (statement.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided"))
            {
                var beingVoided = ((StatementRef)statement.Target).Id;
                if (await IsVoidingAsync(beingVoided))
                {
                    throw new Exception("A Voiding Statement cannot Target another Voiding Statement.");
                }
            }

            //?? _xApiValidationService.ValidateVerb(statement.Verb);
        }

        public async Task<Statement> GetStatementAsync(
            Guid? id,
            bool toGetVoided = false,
            IList<StringWithQualityHeaderValue> acceptLanguages = null,
            string format = "exact"
        )
        {
            if (toGetVoided != await IsVoidedAsync(id))
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

            var filtered = FilterWithLanguage((statements.Count > 0) ? statements[0] : null, acceptLanguages);
            if (format == "canonical")
            {
                filtered = GetCanonicalStatement(filtered);
            }

            return filtered;
        }

        private IList<Statement> FilterWithLanguage(IList<Statement> statements, IList<StringWithQualityHeaderValue> acceptLanguages)
        {
            var filteredStatements = new List<Statement>();
            foreach (var statement in statements)
            {
                filteredStatements.Add(FilterWithLanguage(statement, acceptLanguages));
            }

            return filteredStatements;
        }

        private Statement FilterWithLanguage(Statement statement, IList<StringWithQualityHeaderValue> acceptLanguages)
        {
            if (statement == null
                || acceptLanguages == null
                || acceptLanguages.Count == 0)
            {
                return statement;
            }

            FilterWithLanguage(statement.Target as Activity, acceptLanguages);

            statement.Verb.Display = FilterWithLanguage(statement.Verb.Display, acceptLanguages);

            FilterWithLanguage(statement.Context?.ContextActivities?.Category, acceptLanguages);
            FilterWithLanguage(statement.Context?.ContextActivities?.Grouping, acceptLanguages);
            FilterWithLanguage(statement.Context?.ContextActivities?.Other, acceptLanguages);
            FilterWithLanguage(statement.Context?.ContextActivities?.Parent, acceptLanguages);
            
            if (statement.Attachments != null)
            {
                foreach (var attachment in statement.Attachments)
                {
                    attachment.Display = FilterWithLanguage(attachment.Display, acceptLanguages);
                    attachment.Description = FilterWithLanguage(attachment.Description, acceptLanguages);
                }
            }

            return statement;
        }

        private void FilterWithLanguage(IList<Activity> activities, IList<StringWithQualityHeaderValue> acceptLanguages)
        {
            if (activities == null)
            {
                return;
            }
            
            foreach (var activity in activities)
            {
                FilterWithLanguage(activity, acceptLanguages);
            }
        }

        private void FilterWithLanguage(Activity activity, IList<StringWithQualityHeaderValue> acceptLanguages)
        {
            if (activity != null && activity.Definition != null)
            {
                activity.Definition.Name = FilterWithLanguage(activity.Definition.Name, acceptLanguages);
                activity.Definition.Description = FilterWithLanguage(activity.Definition.Description, acceptLanguages);
                FilterWithLanguage(activity.Definition.Choices, acceptLanguages);
                FilterWithLanguage(activity.Definition.FillIn, acceptLanguages);
                FilterWithLanguage(activity.Definition.LongFillIn, acceptLanguages);
                FilterWithLanguage(activity.Definition.Numeric, acceptLanguages);
                FilterWithLanguage(activity.Definition.Other, acceptLanguages);
                FilterWithLanguage(activity.Definition.Performance, acceptLanguages);
                FilterWithLanguage(activity.Definition.Scale, acceptLanguages);
                FilterWithLanguage(activity.Definition.Sequencing, acceptLanguages);
                FilterWithLanguage(activity.Definition.Source, acceptLanguages);
                FilterWithLanguage(activity.Definition.Steps, acceptLanguages);
                FilterWithLanguage(activity.Definition.Target, acceptLanguages);
                FilterWithLanguage(activity.Definition.TrueFalse, acceptLanguages);
            }
        }

        private void FilterWithLanguage(IList<InteractionComponent> component, IList<StringWithQualityHeaderValue> acceptLanguages)
        {
            if (component == null)
            {
                return;
            }
            
            foreach (var c in component)
            {
                c.Description = FilterWithLanguage(c.Description, acceptLanguages);
            }
        }

        private Dictionary<string, string> FilterWithLanguage(Dictionary<string, string> languageMap, IList<StringWithQualityHeaderValue> acceptLanguages)
        {
            var filteredMap = new Dictionary<string, string>();

            if (languageMap == null)
            {
                return filteredMap;
            }

            foreach (var name in languageMap)
            {
                if (acceptLanguages.Any(x => x.Value == name.Key))
                {
                    filteredMap.Add(name.Key, name.Value);
                }
            }

            return filteredMap;
        }

        private async Task<bool> IsVoidedAsync(Guid? id)
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
                    if (s.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided")
                        && s.Target as StatementRef != null
                        && ((StatementRef)s.Target).Id == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // A is a regular statement. B is voiding A. C is now voiding B.
        // But C can't void B because B is voiding A. 
        private async Task<bool> IsVoidingAsync(Guid? id)
        {
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return false;
            }

            using (var statementsCursor = await collection.FindAsync(x => x.Id == id))
            {
                var voiding = statementsCursor.FirstOrDefault();
                return voiding?.Verb?.Id == new Uri("http://adlnet.gov/expapi/verbs/voided");
            }
        }

        private Statement GetCanonicalStatement(Statement statement)
        {
            return new Statement
            {
                Target = statement.Target,
                Verb = statement.Verb,
                Context = statement.Context
            };
        }

        private Statement GetIdsOnly(Statement statement)
        {
            return new Statement
            {
                Id = statement.Id,
                Actor = new Agent
                {
                    Mbox = statement.Actor?.Mbox
                },
                Verb = new Verb
                {
                    Id = statement.Verb?.Id
                },
                Target = GetIdsOnly(statement.Target),
                Authority = new Agent
                {
                    Mbox = statement.Authority?.Mbox
                }
            };
        }

        private IStatementTarget GetIdsOnly(IStatementTarget target)
        {
            if (target.ObjectType == Activity.OBJECT_TYPE)
            {
                return new Activity
                {
                    ObjectType = null,
                    Id = ((Activity)target)?.Id
                };
            }
            else if (target.ObjectType == Agent.OBJECT_TYPE)
            {
                return new Agent
                {
                    ObjectType = null,
                    Mbox = ((Agent)target)?.Mbox
                };
            }
            else if (target.ObjectType == Group.OBJECT_TYPE)
            {
                return new Group
                {
                    ObjectType = null,
                    Mbox = ((Agent)target)?.Mbox,
                    Member = GetGroupMemberIdsOnly(target as Group)
                };
            }
            else if (target.ObjectType == StatementRef.OBJECT_TYPE)
            {
                return new StatementRef
                {
                    ObjectType = null,
                    Id = ((StatementRef)target)?.Id
                };
            }
            else if (target.ObjectType == SubStatement.OBJECT_TYPE)
            {
                return new SubStatement
                {
                    Actor = GetIdsOnly(((SubStatement)target)?.Actor),
                    Verb = new Verb
                    {
                        Id = ((SubStatement)target)?.Verb?.Id
                    },
                    Target = GetIdsOnly(((SubStatement)target)?.Target)
                };
            }

            return null;
        }

        private Agent GetIdsOnly(Agent agent)
        {
            if (agent.ObjectType == Agent.OBJECT_TYPE)
            {
                return new Agent
                {
                    ObjectType = agent.ObjectType,
                    Mbox = ((Agent)agent)?.Mbox
                };
            }
            else if (agent.ObjectType == Group.OBJECT_TYPE)
            {
                return new Group
                {
                    ObjectType = agent.ObjectType,
                    Mbox = ((Agent)agent)?.Mbox
                };
            }

            return null;
        }

        private List<Agent> GetGroupMemberIdsOnly(Group group)
        {
            var members = new List<Agent>();
            if (group.Member == null)
            {
                return members;
            }

            foreach(var agent in group.Member)
            {
                members.Add(new Agent
                {
                    Mbox = agent?.Mbox
                });
            }

            return members;
        }

        private IList<Statement> GetCanonicalStatements(IList<Statement> statements)
        {
            var canonical = new List<Statement>();

            foreach (var statement in statements)
            {
                canonical.Add(GetCanonicalStatement(statement));
            }

            return canonical;
        }

        private IList<Statement> GetIdsOnly(IList<Statement> statements)
        {
            var idsOnly = new List<Statement>();

            foreach (var statement in statements)
            {
                idsOnly.Add(GetIdsOnly(statement));
            }

            return idsOnly;
        }

        public async Task<StatementsResult> GetStatementsAsync(
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
        )
        {
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return null;
            }

            var filter = Builders<Statement>.Filter.Where(statement =>
                (agent == null || agent.Equals(statement.Actor))
                && (verbId == null || statement.Verb.Id == verbId)
                && (activity == null || statement.Target as Activity != null && ((Activity)statement.Target).Id == activity)
                && (registration == Guid.Empty || statement.Context != null && statement.Context.Registration == registration)
            );

            var cursor = ascending
                ? collection.Find(filter).SortBy(x => x.Stored).Limit(limit)
                : collection.Find(filter).SortByDescending(x => x.Stored).Limit(limit);

            var statements = cursor.ToList();
            if (since != null && since != DateTime.MinValue)
            {
                var sinceUtc = since.ToUniversalTime();
                statements = statements.Where(x => x.Stored >= sinceUtc).ToList();
            }

            if (until != null && until != DateTime.MinValue)
            {
                var untilUtc = until.ToUniversalTime();
                statements = statements.Where(x => x.Stored <= untilUtc).ToList();        
            }

            // Replace every voided statement with a statement that is voiding it.
            statements = await ReplaceVoidedWithVoidingAsync(statements);

            var filtered = FilterWithLanguage(statements, acceptLanguages);
            if (format == "canonical")
            {
                filtered = GetCanonicalStatements(filtered);
            }
            else if (format == "ids")
            {
                filtered = GetIdsOnly(filtered);
            }

            return new StatementsResult(filtered);
        }

        private async Task<List<Statement>> ReplaceVoidedWithVoidingAsync(IList<Statement> statements)
        {
            var newList = new List<Statement>();
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return await Task.FromResult(newList);
            }

            var cursor = await collection.FindAsync<Statement>(x => x.Verb.Id == voidedVerb);
            var voidingStatements = cursor.ToList();
            foreach (var statement in statements)
            {
                var voidingStatement = voidingStatements.Find(x =>
                    x.Target is StatementRef
                    && ((StatementRef)x.Target).Id  == statement.Id);
                if (voidingStatement != null)
                {
                    newList.Add(voidingStatement);
                }
                else
                {
                    newList.Add(statement);
                }
            }

            return newList;
        }

        private async Task<bool> IsTargetVoidedAsync(Statement statement)
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
                    var voidedStatement = await GetStatementAsync(statementRef.Id, true);
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

        public async Task SaveStateAsync(byte[] value, string stateId, string activityId, Agent agent, Guid? registration, string contentType)
        {
            if (value == null)
            {
                return;
            }

            var doc = await GetStateDocumentAsync(stateId, activityId, agent, registration);
            if (contentType == "application/json")
            {
                if (doc != null)
                {
                    if (doc.ContentType == "application/json") // Merge JSONs
                    {
                        var content = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(doc.Content));
                        var contentAdded = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(value));
                        content.Merge(contentAdded);
                        value = Encoding.UTF8.GetBytes(content.ToString());
                    }
                    else
                    {
                        throw new Exception("POST state: The existing state value must have ContentType application/json to update with JSON.");
                    }
                }
            }
            
            var state = new StateDocument
            {
                Id = stateId,
                Etag = DateTime.UtcNow.Ticks.ToString(),
                Timestamp = DateTime.UtcNow,
                Activity = new Activity
                {
                    Id = new Uri(activityId)
                },
                Agent = agent,
                Registration = registration,
                ContentType = contentType,
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

        public async Task<string> GetStateAsync(string stateId, string activityId, Agent agent)
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
            
            return await Task.FromResult(stateAsString);
        }

        public async Task<StateDocument> GetStateDocumentAsync(string stateId, string activityId, Agent agent, Guid? registration)
        {
            var collection = _db.GetCollection<StateDocument>(stateCollection);
            if (collection == null)
            {
                return null;
            }

            var cursor = await collection.FindAsync
            (
                x =>
                    (stateId == null || x.Id == stateId)
                    && x.Activity.Id == new Uri(activityId)
                    && x.Agent.Account.Name == agent.Account.Name
                    && (registration == null || x.Registration == registration)
            );
            var state = await cursor.FirstOrDefaultAsync();

            return state;
        }

        public async Task<IList<StateDocument>> GetStateDocumentsAsync(string activityId, Agent agent, Guid? registration, DateTime? since = null)
        {
            var collection = _db.GetCollection<StateDocument>(stateCollection);
            if (collection == null)
            {
                return null;
            }

            var cursor = await collection.FindAsync
            (
                x => x.Activity.Id == new Uri(activityId)
                    && x.Agent.Account.Name == agent.Account.Name
                    && (registration == null || x.Registration == registration)
                    && (since == null || x.Timestamp >= since)
            );
            var states = cursor.ToList();

            return states;
        }

        public async Task<bool> DeleteStateDocumentAsync(string stateId, string activityId, Agent agent, Guid? registration)
        {
            var collection = _db.GetCollection<StateDocument>(stateCollection);
            if (collection == null)
            {
                return false;
            }

            var deleteResult = await collection.DeleteOneAsync
            (
                x =>
                    (stateId == null || x.Id == stateId)
                    && x.Activity.Id == new Uri(activityId)
                    && x.Agent.Account.Name == agent.Account.Name
                    && (registration == null || x.Registration == registration)
            );

            return deleteResult.IsAcknowledged;
        }

        public async Task SaveActivityProfileAsync(byte[] value, string activityId, string profileId, string contentType)
        {
            if (contentType == "application/json")
            {
                var doc = await GetActivityProfileDocumentAsync(activityId, profileId);
                if (doc != null)
                {
                    if (doc.ContentType == "application/json") // Merge JSONs
                    {
                            var content = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(doc.Content));
                            var contentAdded = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(value));
                            content.Merge(contentAdded);
                            value = Encoding.UTF8.GetBytes(content.ToString());
                        
                    }
                    else
                    {
                        throw new Exception("POST activities/profile: The existing profile value must have ContentType application/json to update with JSON.");
                    }
                }
            }

            var profile = new ActivityProfileDocument
            {
                Id = profileId,
                Etag = DateTime.UtcNow.Ticks.ToString(),
                Timestamp = DateTime.UtcNow,
                Activity = new Activity
                {
                    Id = new Uri(activityId)
                },
                ContentType = contentType,
                Content = value
            };
            
            var collection = _db.GetCollection<ActivityProfileDocument>(activityProfileCollection);

            await collection.FindOneAndReplaceAsync<ActivityProfileDocument>
            (
                x =>
                    x.Id == profileId &&
                    x.Activity.Id == new Uri(activityId),
                profile,
                new FindOneAndReplaceOptions<ActivityProfileDocument>
                {
                    IsUpsert = true
                }
            );
        }

        public async Task<ActivityProfileDocument> GetActivityProfileDocumentAsync(string activityId, string profileId)
        {
            var collection = _db.GetCollection<ActivityProfileDocument>(activityProfileCollection);
            if (collection == null)
            {
                return null;
            }

            var doc = await collection.FindAsync
            (
                x => x.Id == profileId && x.Activity.Id == new Uri(activityId)
            );

            return await doc?.FirstOrDefaultAsync();
        }

        public async Task<IList<ActivityProfileDocument>> GetActivityProfileDocumentsAsync(string activityId, DateTime? since)
        {
            var collection = _db.GetCollection<ActivityProfileDocument>(activityProfileCollection);
            if (collection == null)
            {
                return null;
            }

            var doc = await collection.FindAsync
            (
                x => x.Activity.Id == new Uri(activityId)
                    && (since == null || x.Timestamp >= since)
            );

            return doc.ToList();
        }

        public async Task<bool> DeleteActivityProfileAsync(string activityId, string profileId)
        {
            var collection = _db.GetCollection<ActivityProfileDocument>(activityProfileCollection);
            if (collection == null)
            {
                return false;
            }

            var deleteResult = await collection.DeleteOneAsync
            (
                x =>
                    x.Id == profileId &&
                    x.Activity.Id == new Uri(activityId)
            );

            return deleteResult.IsAcknowledged;
        }

        public async Task SaveAgentProfileAsync(byte[] value, Agent agent, string profileId, string contentType)
        {
            if (contentType == "application/json")
            {
                var doc = await GetAgentProfileDocumentAsync(agent, profileId);
                if (doc != null)
                {
                    if (doc.ContentType == "application/json") // Merge JSONs
                    {
                            var content = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(doc.Content));
                            var contentAdded = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(value));
                            content.Merge(contentAdded);
                            value = Encoding.UTF8.GetBytes(content.ToString());
                    }
                    else
                    {
                        throw new Exception("POST agents/profile: The existing profile value must have ContentType application/json to update with JSON.");
                    }
                }
            }

            var profile = new AgentProfileDocument
            {
                Id = profileId,
                Etag = DateTime.UtcNow.Ticks.ToString(),
                Timestamp = DateTime.UtcNow,
                Agent = agent,
                ContentType = contentType,
                Content = value
            };
            
            var collection = _db.GetCollection<AgentProfileDocument>(agentProfileCollection);

            await collection.FindOneAndReplaceAsync<AgentProfileDocument>
            (
                x =>
                    x.Id == profileId &&
                    x.Agent.Equals(agent),
                profile,
                new FindOneAndReplaceOptions<AgentProfileDocument>
                {
                    IsUpsert = true
                }
            );
        }

        public async Task<AgentProfileDocument> GetAgentProfileDocumentAsync(Agent agent, string profileId)
        {
            var collection = _db.GetCollection<AgentProfileDocument>(agentProfileCollection);
            if (collection == null)
            {
                return null;
            }

            var doc = await collection.FindAsync
            (
                x => x.Id == profileId && x.Agent.Equals(agent)
            );

            return await doc.FirstOrDefaultAsync();
        }

        public async Task<IList<AgentProfileDocument>> GetAgentProfileDocumentsAsync(Agent agent, DateTime? since)
        {
            var collection = _db.GetCollection<AgentProfileDocument>(agentProfileCollection);
            if (collection == null)
            {
                return null;
            }

            var doc = await collection.FindAsync
            (
                x => x.Agent.Equals(agent)
                    && (since == null || x.Timestamp >= since)
            );

            return doc.ToList();
        }

        public async Task<bool> DeleteAgentProfileAsync(Agent agent, string profileId)
        {
            var collection = _db.GetCollection<AgentProfileDocument>(agentProfileCollection);
            if (collection == null)
            {
                return false;
            }

            var deleteResult = await collection.DeleteOneAsync
            (
                x =>
                    x.Id == profileId &&
                    x.Agent.Equals(agent)
            );

            return deleteResult.IsAcknowledged;
        }

        public async Task<Activity> GetActivityAsync(string activityId)
        {
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return null;
            }

            var cursor = await collection.FindAsync
            (
                x =>
                    x.Target as Activity != null
                    && ((Activity)x.Target).Id == new Uri(activityId)
            );

            var statements = cursor.ToList();
            var result = new JObject();
            var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            foreach (var statement in statements)
            {
                var activityJson = JsonConvert.SerializeObject((Activity)statement.Target, jsonSerializerSettings);
                var activityJObject =
                    JsonConvert.DeserializeObject<JObject>(activityJson);
                result.Merge(activityJObject);
            }

            return result.ToObject<Activity>();
        }

        public async Task<Person> GetPersonAsync(Agent agent)
        {
            var person = new Person();
            var collection = _db.GetCollection<Statement>(statementCollection);
            if (collection == null)
            {
                return person;
            }

            var cursor = await collection.FindAsync
            (
                x => x.Actor.Equals(agent)
            );

            var statements = cursor.ToList();
            foreach (var statement in statements)
            {
                if (!string.IsNullOrEmpty(statement.Actor?.Name))
                {
                    person.Name.Add(statement.Actor.Name);
                }
            }

            return person;
        }

        // Admin
        public async Task RegisterUser(UserViewModel user)
        {
            var collection = _db.GetCollection<UserModel>(userCollection);
            if (collection == null || user == null)
            {
                return;
            }

            var userModel = new UserModel
            {
                Id = Guid.NewGuid(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Password = user.Password,
                IsActive = true,
                Tenants = new List<Guid>()
            };

            var tenant = await AddCredentialToTenant(user.Tenant, userModel);
            userModel.Tenants.Add(tenant.Id);

            await collection.InsertOneAsync(userModel);
        }

        public TenantModel GetTenantBySubdomain(string subdomain)
        {
            var collection = _db.GetCollection<TenantModel>(tenantCollection);
            if (collection == null || string.IsNullOrEmpty(subdomain.Trim()))
            {
                return null;
            }

            var cursor = collection.Find(x => x.Name == subdomain);//string.Equals(x.Name, subdomain, StringComparison.OrdinalIgnoreCase));
            
            return cursor.FirstOrDefault();
        }

        public async Task<TenantModel> AddCredentialToTenant(string tenantName, UserModel userModel)
        {
            var collection = _db.GetCollection<TenantModel>(tenantCollection);
            if (collection == null || string.IsNullOrEmpty(tenantName.Trim()))
            {
                return null;
            }

            var cursor = await collection.FindAsync(x => x.Name == tenantName);
            var tenant = cursor.FirstOrDefault();
            if (tenant == null)
            {
                tenant = new TenantModel
                {
                    Id = Guid.NewGuid(),
                    Name = tenantName,
                    LrsCredentials = new List<CredentialModel>(),
                    Users = new List<Guid>()
                };
                await collection.InsertOneAsync(tenant);
            }
            if (tenant.LrsCredentials == null)
            {
                tenant.LrsCredentials = new List<CredentialModel>();
            }
            var credential = new CredentialModel
            {
                Id = Guid.NewGuid(),
                Identifier = "abc",
                Password = "123"
            };
            tenant.LrsCredentials.Add(credential);
            tenant.Users.Add(userModel.Id);

            return await collection.FindOneAndUpdateAsync(
                Builders<TenantModel>.Filter
                    .Eq("Name", tenantName),
                Builders<TenantModel>.Update
                    .Set("LrsCredentials", tenant.LrsCredentials)
                    .Set("Users", tenant.Users));
        }
    }
}
