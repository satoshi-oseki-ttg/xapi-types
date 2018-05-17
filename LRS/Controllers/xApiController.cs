using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using bracken_lrs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using bracken_lrs.Models.xAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Web;
using bracken_lrs.Attributes;
using bracken_lrs.Model;
using bracken_lrs.Models.xAPI.Documents;
using System.Security.Cryptography;

namespace bracken_lrs.Controllers
{
    [Route("tcapi")]
    [Authorize]
    public class xApiController : Controller
    {
        private readonly IxApiService _xApiService;
        private readonly IJobQueueService _jobQueueService;
        private readonly IRepositoryService _repositoryService;
        private readonly ISignedStatementService _signedStatementService;
        private readonly IHttpService _httpService;
        private static readonly Uri completed = new Uri("http://adlnet.gov/expapi/verbs/completed");
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly string[] alternateRequestHeaders = new []
        {
            "Authorization", "X-Experience-API-Version", "Content-Type", "Content-Length", "If-Match", "If-None-Match"
        };

        public xApiController(
            IxApiService xApiService,
            IJobQueueService jobQueueService,
            IRepositoryService repositoryService,
            ISignedStatementService signedStatementService,
            IHttpService httpService)
        {
            _xApiService = xApiService;
            _jobQueueService = jobQueueService;
            _repositoryService = repositoryService;
            _signedStatementService = signedStatementService;
            _httpService = httpService;
        }

        [HttpGet("statements")]
        public async Task<IActionResult> GetStatement(
            [FromQuery] Guid statementId,
            [FromQuery] Guid voidedStatementId,
            [FromQuery] string agent, // JSON
            [FromQuery] Uri verb,
            [FromQuery] Uri activity,
            [FromQuery] Guid registration,
            [FromQuery] bool related_activities,
            [FromQuery] bool related_agents,
            [FromQuery] DateTime since,
            [FromQuery] DateTime until,
            [FromQuery] int limit,
            [FromQuery] string format, // "ids" | "exact" | "canonical"
            [FromQuery] bool attachments,
            [FromQuery] bool ascending)
        {
            Response.Headers.Add("X-Experience-API-Consistent-Through", DateTime.UtcNow.ToString("o"));

            IList<string> invalidParameters;
            var allParametersValid = AreQueryParametersValid(Request.Query.Keys, out invalidParameters);

            if (!allParametersValid)
            {
                return BadRequest($"{string.Join(",", invalidParameters)} are invalid parameters.");
            }

            var lang = Request.Headers["Accept-Language"];
            var acceptLanguages = format == "canonical"
                ? Microsoft.Net.Http.Headers.StringWithQualityHeaderValue.ParseList(lang)
                : null;

            if (statementId != Guid.Empty
                &&
                (!string.IsNullOrEmpty(agent)
                || verb != null
                || activity != null
                || registration != Guid.Empty
                || related_activities
                || related_agents
                || since != DateTime.MinValue
                || until != DateTime.MinValue
                || limit > 0
                || ascending
                )
            )
            {
                return BadRequest("GET can't use filtering parameters (e.g. since, ascending) when statementId is specified.");
            }

            var noIds = statementId == Guid.Empty
                && voidedStatementId == Guid.Empty;
                // && verb == null;
            if (noIds)
            {
                var agentObject = agent != null
                    ? JsonConvert.DeserializeObject<Agent>(agent)
                    : null;
                var result = _repositoryService.GetStatements
                    (agentObject, verb, activity, registration, limit, since, until, acceptLanguages, format, ascending);
                var lastStatementStored = result.Statements.First().Stored.ToString("o");
                if (limit > 0 || since != null && since > DateTime.MinValue)
                {
                    result.More = $"/tcapi/statements?since={lastStatementStored}";
                }

                if (attachments) // return statements in multipart format
                {
                    var multipartContent = _httpService.CreateMultipartContent(result);
                    Request.HttpContext.Response.ContentType = multipartContent.Headers.ContentType.ToString();
                    var content = await multipartContent.ReadAsStreamAsync();
                    return Ok(content);
                }

                Response.Headers.Add("Last-Modified", lastStatementStored);
                return Ok(result);
            }

            var id = voidedStatementId == Guid.Empty ? statementId : voidedStatementId;
            var statement = await _repositoryService.GetStatement(id, voidedStatementId != Guid.Empty, acceptLanguages, format);
            if (statement == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", statement.Stored.ToString("o"));
            return Ok(statement);
        }

        private bool AreQueryParametersValid(ICollection<string> queryPatameters, out IList<string> invalidParameters)
        {
            var validQueryParameters = new [] {
                "statementId",
                "voidedStatementId",
                "agent",
                "verb",
                "activity",
                "registration",
                "related_activities",
                "related_agents",
                "since",
                "until",
                "limit",
                "format",
                "attachments",
                "ascending"
            };

            invalidParameters = new List<string>();
            var result = true;
            foreach (var q in queryPatameters)
            {
                if (!validQueryParameters.Any(x => x == q))
                {
                    invalidParameters.Add(q);
                    result = false;
                }
            }

            return result;
        }

        [HttpHead("statements")]
        [ProducesResponseType(200)]
        public IActionResult HeadStatement()
        {
            return Ok();
        }

        [HttpHead("activities")]
        [ProducesResponseType(200)]
        public IActionResult HeadActivities()
        {
            return Ok();
        }

        [Consumes("multipart/mixed", "multipart/form-data")]
        [PostStatementAction]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpPost("statements")]
        public async Task<IActionResult> PostSignedStatementAsync()
        {
            try
            {
                var statement = await _signedStatementService.GetSignedStatementAsync(Request.Body, Request.ContentType);
                var userName = User.Identity.Name;
                var lrsUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";

                return Ok(await _repositoryService.SaveStatements(statement, null, lrsUrl, userName));
            }
            catch (Exception e)
            {
                return await Task.FromResult(BadRequest(e));
            }
        }

        [AllowAnonymous]
        [PostStatementAction]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpPost("statements")]
        public async Task<IActionResult> PostStatementWithHttpMethodAsync([FromForm]IFormCollection query, [FromQuery]string method)
        {
            if (Request.Query.Count != 1)
            {
                return BadRequest("An LRS will reject an alternate request syntax which contains any extra information with error code 400 Bad Request.");
            }

            if (method == "PUT" || method == "GET")
            {
                try
                {
                    var response = await ResendStatementRequest(Request, query, method);
                    return response;
                }
                catch (Exception e)
                {
                    return BadRequest(e);
                }
            }

            return BadRequest("An alternate request only supports GET and POST.");
        }

        private async Task<IActionResult> ResendStatementRequest(HttpRequest request, IFormCollection formData, string method)
        {
            // var statementId = new Guid(formData["statementId"]);
            using (var client = new HttpClient())
            {
                var httpRequest = new HttpRequestMessage(new HttpMethod(method),
                    AddQueryParamsToUri(new Uri($"{request.Scheme}://{request.Host}/tcapi/statements"), formData));
                CopyAndAssignHeaders(request.Headers, httpRequest.Headers, formData);
                if (formData.Keys.Contains("content"))
                {
                    httpRequest.Content = new StringContent(HttpUtility.UrlDecode(formData["content"]), Encoding.UTF8, "application/json");
                }
                var response = await client.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest();
                }
                return new ObjectResult(await response.Content.ReadAsStringAsync())
                {
                    StatusCode = (int?)response.StatusCode
                };
            }
        }

        private Uri AddQueryParamsToUri(Uri uri, IFormCollection formData)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var param in formData)
            {
                if (Array.IndexOf(alternateRequestHeaders, param.Key) == -1
                    && param.Key != "content")
                {
                    query[param.Key] = param.Value;
                }
            }
            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }

        private void CopyAndAssignHeaders(IHeaderDictionary sourceHeaders, HttpRequestHeaders newHeaders, IFormCollection query)
        {
            foreach (var header in sourceHeaders)
            {
                if (!header.Key.StartsWith("Content-"))
                {
                    if (header.Key == "Authorization")
                    {
                        newHeaders.Authorization = AuthenticationHeaderValue.Parse(header.Value);
                    }
                    else
                    {
                        newHeaders.Add(header.Key, header.Value.ToString());
                    }
                }
            }

            foreach (var header in alternateRequestHeaders)
            {
                if (query.ContainsKey(header))
                {
                    if (newHeaders.Contains(header))
                    {
                        newHeaders.Remove(header);
                    }
                    if (header == "Authorization")
                    {
                        newHeaders.Authorization = AuthenticationHeaderValue.Parse(HttpUtility.UrlDecode((query[header])));
                    }
                    else
                    {
                        newHeaders.Add(header, query[header].ToString());
                    }
                }
            }
        }

        [Consumes("application/json")]
        [PostStatementAction]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpPost("statements")]
        public async Task<IActionResult> PostStatement([FromBody]object obj)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userName = User.Identity.Name;
                var lrsUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";

                return Ok(await _repositoryService.SaveStatements(obj, null, lrsUrl, userName));
            }
            catch (Exception e)
            {
                return await Task.FromResult(BadRequest(e));
            }
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpPut("statements")]
        public async Task<IActionResult> PutStatement([FromBody]object obj, [FromQuery]Guid statementId, [FromQuery]Guid publishedResultID)
        {
            IList<string> invalidParameters;
            var allParametersValid = AreQueryParametersValid(Request.Query.Keys, out invalidParameters);
            if (!allParametersValid)
            {
                return BadRequest($"{string.Join(",", invalidParameters)} are invalid parameters.");
            }

            if (statementId == Guid.Empty)
            {
                return await Task.FromResult(BadRequest("There's no statementId supplied in a PUT statement request."));
            }

            try
            {
                var userName = User.Identity.Name;
                var lrsUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";
                await _repositoryService.SaveStatements(obj, statementId, lrsUrl, userName);
                return NoContent();
            }
            catch (Exception e)
            {
                return await Task.FromResult(BadRequest(e));
            }
            // var json = value.ToString();
            // //_xApiService.SaveStatement(value, statementId);
            // _jobQueueService.EnqueueStatement(value);
            // var verb = value["verb"];
            // var verbId = verb["id"];
            // //var statement = new Statement(value);
            // //if (statement.verb.Id == completed)
            // if (verbId.Value<string>() == completed.ToString())
            // {
            //     var url = $"http://live.brackenlearning.com.satoshi.work/statements?statementId={statementId}&publishedResultID={publishedResultID}";
            //     var content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
            //     httpClient.DefaultRequestHeaders.Authorization =
            //         new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
            //             System.Text.ASCIIEncoding.ASCII.GetBytes("bracken:welcome123")));
            //     httpClient.PutAsync(url, content);
            // }
        }

        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpPut("statements")]
        public async Task<IActionResult> PutStatementWithHttpMethodAsync([FromForm]IFormCollection query, [FromQuery]string method)
        {
            if (method != null)
            {
                return await Task.FromResult(BadRequest("An LRS rejects an alternate request syntax not issued as a POST"));
            }

            return await Task.FromResult(NoContent());
        }

        [HttpHead("activities/state")]
        public IActionResult HeadState([FromQuery]string stateId, [FromQuery]string activityId, [FromQuery]string agent)
        {
            return Ok();
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpPost("activities/state")]
        public async Task<IActionResult> PostState(
            [FromQuery]string stateId,
            [FromQuery]string activityId,
            [FromQuery]string agent,
            [FromQuery]string registration)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                return BadRequest("POST activities/state: The stateId parameter must be supplied.");
            }
            
            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("POST activities/state: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("POST activities/state: The agent parameter must be supplied.");
            }

            Guid? registrationGuid = null;
            try
            {
                if (registration != null)
                {
                    registrationGuid = Guid.Parse(registration);
                }
            }
            catch (FormatException)
            {
                return BadRequest("POST activities/state: The registration parameter must be a valid UUID.");
            }

            if (Request.ContentType == "application/json")
            {
                var stateContent = await new StreamContent(Request.Body).ReadAsByteArrayAsync();
                try
                {
                    JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(stateContent));
                    var agentObject = JsonConvert.DeserializeObject<Agent>(agent);
                    await _repositoryService.SaveState(stateContent, stateId, activityId, agentObject, registrationGuid, Request.ContentType);

                    return NoContent();
                }
                catch (JsonException)
                {
                    return BadRequest("POST activities/state: The state value must be a valid JSON.");
                }
                catch (Exception e)
                {
                    return BadRequest(e.ToString());
                }
            }
            else
            {
                return BadRequest("POST activities/state: The ContentType must be 'application/json'.");
            }

            // using (var ms = new MemoryStream(2048))
            // {
            //     Request.Body.CopyTo(ms);
            //     var value = ms.ToArray();
            //     var agentObject = JsonConvert.DeserializeObject<Agent>(agent);
            //     //_xApiService.SaveState(value, stateId, activityId, agent);
            //     //_jobQueueService.EnqueueState(value, stateId, activityId, agent);
            //     await _repositoryService.SaveState(value, stateId, activityId, agentObject, Request.ContentType);

            //     return NoContent();
            // }
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpPut("activities/state")]
        public async Task<IActionResult> PutState(
            [FromQuery]string stateId,
            [FromQuery]string activityId,
            [FromQuery]string agent,
            [FromQuery]string registration)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                return BadRequest("PUT activities/state: The stateId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("PUT activities/state: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("PUT activities/state: The agent parameter must be supplied.");
            }

            Guid? registrationGuid = null;
            try
            {
                if (registration != null)
                {
                    registrationGuid = Guid.Parse(registration);
                }
            }
            catch (FormatException)
            {
                return BadRequest("PUT activities/state: The registration parameter must be a valid UUID.");
            }

            using (var ms = new MemoryStream(2048))
            {
                Request.Body.CopyTo(ms);
                var value = ms.ToArray();
                try
                {
                    var agentObject = JsonConvert.DeserializeObject<Agent>(agent);
                    //_xApiService.SaveState(value, stateId, activityId, agent);
                    //_jobQueueService.EnqueueState(value, stateId, activityId, agent);
                    await  _repositoryService.SaveState(value, stateId, activityId, agentObject, registrationGuid, Request.ContentType);
                    return NoContent();
                }
                catch (JsonException)
                {
                    return BadRequest("PUT activities/state: The agent parameter must be a valid JSON.");
                }
            }
        }

        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpGet("activities/state")]
        public async Task<IActionResult> GetState(
            [FromQuery]string stateId,
            [FromQuery]string activityId,
            [FromQuery]string agent,
            [FromQuery]string since,
            [FromQuery]string registration
        )
        {
            Response.Headers.Add("X-Experience-API-Consistent-Through", DateTime.UtcNow.ToString("o"));

            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("GET activities/state: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("GET activities/state: The agent parameter must be supplied.");
            }

            Guid? registrationGuid = null;
            try
            {
                if (registration != null)
                {
                    registrationGuid = Guid.Parse(registration);
                }
            }
            catch (FormatException)
            {
                return BadRequest("GET activities/state: The registration parameter must be a valid UUID.");
            }

            DateTime? sinceDateTime = null;
            try
            {
                if (since != null)
                {
                    sinceDateTime = DateTime.Parse(since);
                }
            }
            catch (FormatException)
            {
                return BadRequest("GET activities/state: The since parameter isn't a valid DateTime.");
            }

            Agent agentObject = null;
            try
            {
                agentObject = JsonConvert.DeserializeObject<Agent>(agent);
                if (!agentObject.IsValid())
                {
                    return BadRequest("GET activities/state: The agent parameter must be be uniquely identifiable.");
                }
            }
            catch (JsonException)
            {
                return BadRequest("GET activities/state: The agent parameter must be a valid JSON.");
            }

            if (stateId != null)
            {
                var doc = await _repositoryService.GetStateDocument(stateId, activityId, agentObject, registrationGuid);
                var stateAsString = doc?.Content != null ? System.Text.Encoding.UTF8.GetString(doc.Content) : null;
                if (stateAsString == null)
                {
                    return NotFound();
                }

                if (doc.ContentType == "application/json")
                {
                    return Ok(JsonConvert.DeserializeObject<JObject>(stateAsString));
                }
                else
                {
                    return Ok(stateAsString);
                }
            }
            else
            {
                var docs = await _repositoryService.GetStateDocuments(activityId, agentObject, registrationGuid, sinceDateTime);
                var states = new List<string>(); // returns a list of ids
                foreach (var doc in docs)
                {
                    states.Add(doc.Id);
                }

                return Ok(states);
            }
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpDelete("activities/state")]
        public async Task<IActionResult> DeleteState(
            [FromQuery]string stateId,
            [FromQuery]string activityId,
            [FromQuery]string agent,
            [FromQuery]string registration)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("DELETE activities/state: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("DELETE activities/state: The agent parameter must be supplied.");
            }

            Agent agentObject = null;
            try
            {
                agentObject = JsonConvert.DeserializeObject<Agent>(agent);
                if (!agentObject.IsValid())
                {
                    return BadRequest("GET activities/state: The agent parameter must be be uniquely identifiable.");
                }
            }
            catch (JsonException)
            {
                return BadRequest("GET activities/state: The agent parameter must be a valid JSON.");
            }

            Guid? registrationGuid = null;
            try
            {
                if (registration != null)
                {
                    registrationGuid = Guid.Parse(registration);
                }
            }
            catch (FormatException)
            {
                return BadRequest("GET activities/state: The registration parameter must be a valid UUID.");
            }

            var isAcknowledged = await _repositoryService.DeleteStateDocument(stateId, activityId, agentObject, registrationGuid);

            return NoContent();
        }

        [ProducesResponseType(200)]
        [HttpHead("activities/profile")]
        public IActionResult HeadActivityProfile([FromQuery]Guid activityId)
        {
            return Ok();
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [HttpPost("activities/profile")]
        public async Task<IActionResult> PostActivityProfile([FromQuery]string activityId, [FromQuery]string profileId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("POST activities/profile: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("POST activities/profile: The agent parameter must be supplied.");
            }

            if (Request.ContentType == "application/json")
            {
                var profile = await new StreamContent(Request.Body).ReadAsByteArrayAsync();
                try
                {
                    JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(profile));
                    await _repositoryService.SaveActivityProfile(profile, activityId, profileId, Request.ContentType);

                    return NoContent();
                }
                catch (Exception)
                {
                    return BadRequest("POST activities/profile: The existing profile value must have ContentType application/json to update with JSON.");
                }
            }
            else
            {
                return BadRequest("POST activities/profile: The ContentType must be 'application/json'.");
            }
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(412)]
        [HttpPut("activities/profile")]
        public async Task<IActionResult> PutActivityProfile([FromQuery]string activityId, [FromQuery]string profileId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("PUT activities/profile: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("PUT activities/profile: The agent parameter must be supplied.");
            }

            var ifMatchHeader = Request.Headers["If-Match"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ifMatchHeader))
            {
                var saved = await _repositoryService.GetActivityProfileDocument(activityId, profileId);
                var etag = _httpService.GetETag(System.Text.Encoding.UTF8.GetString(saved.Content));
                if (ifMatchHeader != etag)
                {
                    return StatusCode(412, "PUT activities/profile request is received without either header for a resource that already exists.");
                }
            }

            var ifNoneMatchHeader = Request.Headers["If-None-Match"].FirstOrDefault();
            if (ifNoneMatchHeader == "*")
            {
                var saved = await _repositoryService.GetActivityProfileDocument(activityId, profileId);
                if (saved != null)
                {
                    return StatusCode(412, "PUT activities/profile request is received without either header for a resource that already exists.");
                }
            }

            if (string.IsNullOrEmpty(ifMatchHeader) && string.IsNullOrEmpty(ifNoneMatchHeader))
            {
                var saved = await _repositoryService.GetActivityProfileDocument(activityId, profileId);
                if (saved != null)
                {
                    return StatusCode(409, "PUT activities/profile request is received without either header for a resource that already exists.");
                }
                else
                {
                    return BadRequest("PUT activities/profile request is received without either header.");
                }
            }

            var profile = await new StreamContent(Request.Body).ReadAsByteArrayAsync();
            if (Request.ContentType == "application/json")
            {
                try
                {
                    JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(profile));
                    await _repositoryService.SaveActivityProfile(profile, activityId, profileId, Request.ContentType);

                    return NoContent();
                }
                catch (JsonException)
                {
                    return BadRequest("PUT activities/profile: The state value must be a valid JSON.");
                }
            }
            else
            {
                await _repositoryService.SaveActivityProfile(profile, activityId, profileId, Request.ContentType);

                return NoContent();
            }
        }

        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpGet("activities/profile")]
        public async Task<IActionResult> GetActivityProfile([FromQuery]string activityId, [FromQuery]string profileId, [FromQuery]string since = null)
        {
            Response.Headers.Add("X-Experience-API-Consistent-Through", DateTime.UtcNow.ToString("o"));

            DateTime? sinceDateTime = null;
            try
            {
                if (since != null)
                {
                    sinceDateTime = DateTime.Parse(since);
                }
            }
            catch (FormatException)
            {
                return BadRequest("The since parameter isn't a valid DateTime.");
            }

            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("GET activities/profile: The activityId parameter must be supplied.");
            }
            if (profileId != null)
            {
                var doc = await _repositoryService.GetActivityProfileDocument(activityId, profileId);
                var profileAsString = doc.Content != null ? System.Text.Encoding.UTF8.GetString(doc.Content) : null;
                if (profileAsString == null)
                {
                    return NotFound();
                }

                Response.Headers.Add("ETag", _httpService.GetETag(profileAsString));
                if (doc.ContentType == "application/json")
                {
                    return Ok(JsonConvert.DeserializeObject<JObject>(profileAsString));
                }
                else
                {
                    return Ok(profileAsString);
                }
            }
            else
            {
                var docs = await _repositoryService.GetActivityProfileDocuments(activityId, sinceDateTime);
                var content = new List<string>(); // returns ids
                foreach (var profile in docs)
                {
                    content.Add(profile.Id);
                }

                Response.Headers.Add("ETag", _httpService.GetETag(content.ToArray()));
                return Ok(content);
            }
        }

        [ProducesResponseType(204)]
        [HttpDelete("activities/profile")]
        public async Task<IActionResult> DeleteActivityProfile([FromQuery]string activityId, [FromQuery]string profileId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("DELETE activities/profile: The activityId parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("DELETE activities/profile: The agent parameter must be supplied.");
            }

            await _repositoryService.DeleteActivityProfile(activityId, profileId);

            return NoContent();
        }

        [ProducesResponseType(200)]
        [HttpHead("agents")]
        public IActionResult HeadAgents([FromQuery]Guid agent)
        {
            return Ok();
        }

        [ProducesResponseType(200)]
        [HttpHead("agents/profile")]
        public IActionResult HeadAgentsProfile([FromQuery]Guid activityId)
        {
            return Ok();
        }

        [ProducesResponseType(204)]
        [HttpPost("agents/profile")]
        public async Task<IActionResult> PostAgentsProfile([FromQuery]string agent, [FromQuery]string profileId)
        {
            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("POST agents/profile: The agent parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("POST agents/profile: The agent parameter must be supplied.");
            }

            Agent agentObject = null;
            try
            {
                agentObject = JsonConvert.DeserializeObject<Agent>(agent);
            }
            catch (JsonException)
            {
                return BadRequest("The agent parameter must be a valid JSON.");
            }

            if (Request.ContentType == "application/json")
            {
                var profile = await new StreamContent(Request.Body).ReadAsByteArrayAsync();
                try
                {
                    JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(profile));
                    await _repositoryService.SaveAgentProfile(profile, agentObject, profileId, Request.ContentType);

                    return NoContent();
                }
                catch (Exception)
                {
                    return BadRequest("POST agents/profile: The existing profile value must have ContentType application/json to update with JSON.");
                }
            }
            else
            {
                return BadRequest("POST agents/profile: The ContentType must be 'application/json'.");
            }
        }

        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(412)]
        [HttpPut("agents/profile")]
        public async Task<IActionResult> PutAgentsProfile([FromQuery]string agent, [FromQuery]string profileId)
        {
            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("PUT agents/profile: The agent parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("PUT agents/profile: The agent parameter must be supplied.");
            }

            Agent agentObject = null;
            try
            {
                agentObject = JsonConvert.DeserializeObject<Agent>(agent);
            }
            catch (JsonException)
            {
                return BadRequest("PUT agents/profile: The agent parameter must be a valid JSON.");
            }

            var ifMatchHeader = Request.Headers["If-Match"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ifMatchHeader))
            {
                var saved = await _repositoryService.GetAgentProfileDocument(agentObject, profileId);
                var etag = _httpService.GetETag(System.Text.Encoding.UTF8.GetString(saved.Content));
                if (ifMatchHeader != etag)
                {
                    return StatusCode(412, "PUT agents/profile request is received without either header for a resource that already exists.");
                }
            }

            var ifNoneMatchHeader = Request.Headers["If-None-Match"].FirstOrDefault();
            if (ifNoneMatchHeader == "*")
            {
                var saved = await _repositoryService.GetAgentProfileDocument(agentObject, profileId);
                if (saved != null)
                {
                    return StatusCode(412, "PUT agents/profile equest is received without either header for a resource that already exists.");
                }
            }

            if (string.IsNullOrEmpty(ifMatchHeader) && string.IsNullOrEmpty(ifNoneMatchHeader))
            {
                var saved = await _repositoryService.GetAgentProfileDocument(agentObject, profileId);
                if (saved != null)
                {
                    return StatusCode(409, "PUT agents/profile request is received without either header for a resource that already exists.");
                }
                else
                {
                    return BadRequest("PUT agents/profile request is received without either header.");
                }
            }

            var profile = await new StreamContent(Request.Body).ReadAsByteArrayAsync();
            if (Request.ContentType == "application/json")
            {
                try
                {
                    JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(profile));
                    await _repositoryService.SaveAgentProfile(profile, agentObject, profileId, Request.ContentType);

                    return NoContent();
                }
                catch (JsonException)
                {
                    return BadRequest("PUT agents/profile: The state value must be a valid JSON.");
                }
            }
            else
            {
                await _repositoryService.SaveAgentProfile(profile, agentObject, profileId, Request.ContentType);

                return NoContent();
            }
        }

        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpGet("agents/profile")]
        public async Task<IActionResult> GetAgentsProfile([FromQuery]string agent, [FromQuery]string profileId, [FromQuery]string since)
        {
            Response.Headers.Add("X-Experience-API-Consistent-Through", DateTime.UtcNow.ToString("o"));

            DateTime? sinceDateTIme = null;
            try
            {
                if (since != null)
                {
                    sinceDateTIme = DateTime.Parse(since);
                }
            }
            catch (FormatException)
            {
                return BadRequest("The since parameter isn't a valid DateTime.");
            }

            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("GET agents/profile: The agent parameter must be supplied.");
            }

            Agent agentObject = null;
            try
            {
                agentObject = JsonConvert.DeserializeObject<Agent>(agent);
            }
            catch (JsonException)
            {
                return BadRequest("The agent parameter must be a valid JSON.");
            }

            if (profileId != null)
            {
                var doc = await _repositoryService.GetAgentProfileDocument(agentObject, profileId);
                var profileAsString = doc.Content != null ? System.Text.Encoding.UTF8.GetString(doc.Content) : null;
                if (profileAsString == null)
                {
                    return NotFound();
                }

                Response.Headers.Add("ETag", _httpService.GetETag(profileAsString));
                if (doc.ContentType == "application/json")
                {
                    return Ok(JsonConvert.DeserializeObject<JObject>(profileAsString));
                }
                else
                {
                    return Ok(profileAsString);
                }
            }
            else
            {
                var docs = await _repositoryService.GetAgentProfileDocuments(agentObject, sinceDateTIme);
                var content = new List<string>(); // returns ids
                foreach (var profile in docs)
                {
                    content.Add(profile.Id);
                }
                Response.Headers.Add("ETag", _httpService.GetETag(content.ToArray()));

                return Ok(content);
            }
        }


        [ProducesResponseType(204)]
        [HttpDelete("agents/profile")]
        public async Task<IActionResult> DeleteAgentProfile([FromQuery]string agent, [FromQuery]string profileId)
        {
            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("DELETE agents/profile: The agent parameter must be supplied.");
            }

            if (string.IsNullOrEmpty(profileId))
            {
                return BadRequest("DELETE agents/profile: The agent parameter must be supplied.");
            }

            Agent agentObject = null;
            try
            {
                agentObject = JsonConvert.DeserializeObject<Agent>(agent);
            }
            catch (JsonException)
            {
                return BadRequest("The agent parameter must be a valid JSON.");
            }

            await _repositoryService.DeleteAgentProfile(agentObject, profileId);

            return NoContent();
        }

        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpGet("activities")]
        public async Task<IActionResult> GetActivity([FromQuery]string activityId)
        {
            if (string.IsNullOrEmpty(activityId))
            {
                return BadRequest("The activityId parameter must be supplied.");
            }

            var activity = await _repositoryService.GetActivity(activityId);

            return Ok(activity);
        }

        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents([FromQuery]string agent)
        {
            if (string.IsNullOrEmpty(agent))
            {
                return BadRequest("GET agents: The agent parameter must be supplied.");
            }

            try
            {
                var agentObject = JsonConvert.DeserializeObject<Agent>(agent);
                if (!agentObject.IsValid())
                {
                    return BadRequest("GET agents: The agent parameter must be be uniquely identifiable.");
                }
                var person = await _repositoryService.GetPerson(agentObject);
                return Ok(person);
            }
            catch (JsonException)
            {
                return BadRequest("GET agents: The agent parameter must be a valid JSON.");
            }
        }

        [AllowAnonymous]
        [ProducesResponseType(200)]
        [HttpGet("about")]
        public IActionResult GetAbout()
        {
            return Ok(new { version = new [] { "1.0.3" }});
        }
    }
}
