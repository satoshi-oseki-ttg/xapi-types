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

namespace bracken_lrs.Services
{
    public class xApiService : IxApiService
    {
        private readonly Uri courseType = new Uri("http://adlnet.gov/expapi/activities/course");
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly AppSettings _appSettings;
        private const string dbName = "bracken_lrs"; // This should be per site and each site has collections, states, statements etc?
        private readonly IViewModelService _viewModelService;

        public xApiService(IOptions<AppSettings> optionsAccessor, IViewModelService viewModelService)
        {
            _appSettings = optionsAccessor.Value;
            _viewModelService = viewModelService;
            
            _client = new MongoClient(_appSettings.MongoDbConnection);
            _db = _client.GetDatabase(dbName);
        }
        
        public void SaveStatement(JObject statement, Guid? statementId = null)
        {
            if (statement == null)
            {
                return;
            }

            var doc = BsonDocument.Parse(statement.ToString());
            //var statementObject = new Statement(statement);

            _db.GetCollection<BsonDocument>("statements", new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy })
               .InsertOne(doc);
            // _db.GetCollection<Statement>("statements", new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy })
            //     .InsertOne(statementObject);

            _viewModelService.Update(statement);
        }

        public void SaveState(byte[] value, string stateId, string activityId, string agent)
        {
            if (value == null)
            {
                return;
            }

            var state = new StateDocument();
            state.id = stateId;
            state.activity = new Activity();
            state.activity.Id = activityId;
            state.agent = new Agent(new StringOfJSON(agent).toJObject());
            state.content = value;

            var bson = new BsonDocument();
            bson.Add(new BsonElement("id", stateId));
            var activity = new BsonDocument();
            activity.Add(new BsonElement("id", activityId));
            bson.Add(new BsonElement("activity", activity));
            var actor = BsonDocument.Parse(agent);
            bson.Add("agent", actor);
            bson.Add("content", value);

            var collection = _db.GetCollection<BsonDocument>("states", new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy });

            collection.FindOneAndReplace<BsonDocument>
            (
                x => x["id"] == stateId && x["activity"]["id"] == activityId && x["agent"]["account"]["name"] == state.agent.account.name,
                bson,
                new FindOneAndReplaceOptions<BsonDocument>
                {
                    IsUpsert = true
                }
            );
        }

        public string GetState2(string stateId, string activityId, string agent)
        {
            var collection = _db.GetCollection<StateDocument>("states", new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy });
            if (collection == null)
            {
                return null;
            }

            var agentObject = new Agent(new StringOfJSON(agent).toJObject());
            var state = collection.Find(x => x.id == stateId && x.activity.id == activityId && x.agent.name == agentObject.name).FirstOrDefault();
            
            var bytes = state?.content;
            string str = bytes != null ? System.Text.Encoding.UTF8.GetString(bytes) : "";
            
            return str;
        }
        public string GetState(string stateId, string activityId, string agent)
        {
            var collection = _db.GetCollection<BsonDocument>("states", new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy });
            if (collection == null)
            {
                return null;
            }

            var agentObject = new Agent(new StringOfJSON(agent).toJObject());
            var state = collection.Find(x => x["id"] == stateId && x["activity"]["id"] == activityId && x["agent"]["name"] == agentObject.name).FirstOrDefault();
            
            if (state == null)
            {
                return string.Empty;
            }
            
            var bytes = state["content"];
            var stateAsString = bytes != null ? System.Text.Encoding.UTF8.GetString(bytes.AsBsonBinaryData.Bytes) : null;
            
            return stateAsString;
        }

        public Statement[] GetStatements()
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }
            
            var statements = collection.Find(new BsonDocument()).SortByDescending(x => x.timestamp).ToList();

            return statements.ToArray();
        }

        public Statement GetStatement2(Guid id)
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }
            var filter = Builders<Statement>.Filter.Eq("id", id);
            var idString = id.ToString().ToUpper();
            var st = collection.Find(x => x.id == id).ToList();
            
            return st[0]; //collection.Find(x => x.id == id).FirstOrDefault();
        }

        public Statement GetStatement(Guid id)
        {
            var collection = _db.GetCollection<BsonDocument>("statements");
            if (collection == null)
            {
                return null;
            }
            var idString = id.ToString();
            var statements = collection.Find(x => x["id"] == idString).ToList();

            return (statements.Count > 0) ? BsonSerializer.Deserialize<Statement>(statements[0]) : null;
        }

        public Agent[] GetActors()
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }

            var attemptedStatements = collection.Find<Statement>(x => x.verb.id == "http://adlnet.gov/expapi/verbs/attempted").ToList();
            //var statements = collection.Find<Statement>(x => x.actor.account != null).ToList();
            var actors = (from s in attemptedStatements select s.actor).ToList();
            IEnumerable<Agent> filteredList = actors
                .GroupBy(x => x.account.name)
                .Select(group => group.First());

            return filteredList.ToArray();
        }

        public Agent[] GetActorsInCourse(string courseName)
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }

            var attemptedStatements = collection.Find<Statement>(
                x => x.verb.id == "http://adlnet.gov/expapi/verbs/attempted" &&
                    x.statementObject.id == courseName).ToList();
            //var statements = collection.Find<Statement>(x => x.actor.account != null).ToList();
            var actors = (from s in attemptedStatements select s.actor).ToList();
            IEnumerable<Agent> filteredList = actors
                .GroupBy(x => x.account.name)
                .Select(group => group.First());

            return filteredList.ToArray();
        }

        public StatementObject[] GetCourseStatements()
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }

            var attemptedStatements = collection.Find<Statement>(x => x.verb.id == "http://adlnet.gov/expapi/verbs/attempted").ToList();
            //var statements = collection.Find<Statement>(x => x.actor.account != null).ToList();
            var courses = (from s in attemptedStatements select s.statementObject).ToList();
            IEnumerable<StatementObject> filteredList = courses
                .GroupBy(x => x.Id)
                .Select(group => group.First());

            return filteredList.ToArray();
        }

        public StatementObject[] GetCourseStatements(string username)
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }

            var courseStatements = collection.Find<Statement>
            (
                x => x.statementObject.definition.type == courseType &&
                     x.actor.account.name == username
            ).ToList();
            var courses = (from s in courseStatements select s.statementObject).ToList();
            IEnumerable<StatementObject> filteredList = courses
                .GroupBy(x => x.Id)
                .Select(group => group.First());

            return filteredList.ToArray();
        }

        public Statement[] GetCourseUserStatements(string courseName, string username)
        {
            var collection = _db.GetCollection<Statement>("statements");
            if (collection == null)
            {
                return null;
            }

            var courseStatements = collection.Find<Statement>
            (
                x => x.statementObject.id.StartsWith(courseName) &&
                     x.actor.account.name == username
            ).ToList();

            return courseStatements.ToArray();
        }
    }
}
