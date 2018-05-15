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
        private readonly ISignedStatementService _signedStatementService;

        public RepositoryService
        (
            IOptions<AppSettings> optionsAccessor,
            IxApiValidationService xApiValidationService,
            ISignedStatementService signedStatementService
        )
        {
            _appSettings = optionsAccessor.Value;
            _xApiValidationService = xApiValidationService;
            _signedStatementService = signedStatementService;
            
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

        public async Task<string[]> SaveStatements(object obj, Guid? statementId, string lrsUrl, string userName)
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
            foreach (var jObject in jObjects)
            {
                var id = await SaveStatement(jObject as JObject, statementId, lrsUrl, userName);
                ids.Add(id);
            }

            return ids.ToArray();
        }

        public async Task<string[]> SaveStatement(Statement statement, Guid? statementId, string lrsUrl, string userName)
        {
            return new [] { await DoSaveStatement(statement, statementId, lrsUrl, userName) };
        }

        private async Task<string> SaveStatement(JObject jObject, Guid? statementId, string lrsUrl, string userName)
        {
            _xApiValidationService.ValidateStatement(jObject);

            var statement = JsonConvert.DeserializeObject<Statement>(jObject.ToString());

            return await DoSaveStatement(statement, statementId, lrsUrl, userName);
        }

        private async Task<string> DoSaveStatement(Statement statement, Guid? statementId, string lrsUrl, string userName)
        {
            await ValidateStatement(statement);

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

        private async Task ValidateStatement(Statement statement)
        {
            if (statement.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided") // this is done in StatementBase model
                && statement.Target as StatementRef == null)
            {
                throw new Exception("StatementRef isn't set for verb 'voided'.");
            }

            if (statement.Verb.Id == new Uri("http://adlnet.gov/expapi/verbs/voided"))
            {
                var beingVoided = ((StatementRef)statement.Target).Id;
                if (await IsVoiding(beingVoided))
                {
                    throw new Exception("A Voiding Statement cannot Target another Voiding Statement.");
                }
            }

            //?? _xApiValidationService.ValidateVerb(statement.Verb);
        }

        public async Task<Statement> GetStatement
        (
            Guid? id,
            bool toGetVoided = false,
            IList<StringWithQualityHeaderValue> acceptLanguages = null,
            string format = "exact"
        )
        {
            if (toGetVoided != await IsVoided(id))
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
        private async Task<bool> IsVoiding(Guid? id)
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

        public StatementsResult GetStatements
        (
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
