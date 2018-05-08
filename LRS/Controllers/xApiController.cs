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
        private static readonly Uri completed = new Uri("http://adlnet.gov/expapi/verbs/completed");

        private static readonly HttpClient httpClient = new HttpClient();

        public xApiController
        (
            IxApiService xApiService,
            IJobQueueService jobQueueService,
            IRepositoryService repositoryService,
            ISignedStatementService signedStatementService
        )
        {
            _xApiService = xApiService;
            _jobQueueService = jobQueueService;
            _repositoryService = repositoryService;
            _signedStatementService = signedStatementService;
        }

        [HttpGet("statements")]
        public async Task<IActionResult> GetStatement
        (
            [FromQuery] Guid statementId,
            [FromQuery] Guid voidedStatementId,
            [FromQuery] int limit,
            [FromQuery] DateTime since,
            [FromQuery] Uri verb
        )
        {
            Response.Headers.Add("X-Experience-API-Consistent-Through", DateTime.UtcNow.ToString("o"));

            var noIds = statementId == Guid.Empty
                && voidedStatementId == Guid.Empty
                && verb == null;
            if (noIds)
            {
                var result = _repositoryService.GetStatements(limit, since);
                if (limit > 0 || since != null)
                {
                    var lastStatementStored = result.Statements.First().Stored.ToString("o");
                    result.More = $"/tcapi/statements?since={lastStatementStored}";
                }
                return Ok(result);
            }
            
            if (verb != null)
            {
                var result = _repositoryService.GetStatements(verb);
                return Ok(result);
            }

            var id = voidedStatementId == Guid.Empty ? statementId : voidedStatementId;
            var statement = await _repositoryService.GetStatement(id, voidedStatementId != Guid.Empty);
            if (statement == null)
            {
                return NotFound();
            }

            return Ok(statement);
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

        [Consumes("multipart/mixed")]
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

        [Consumes("application/json")]
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
        [HttpPut("statements")]
        public async Task<IActionResult> PutStatement([FromBody]object obj, [FromQuery]Guid statementId, [FromQuery]Guid publishedResultID)
        {
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

        [HttpPut("activities/state")]
        public async Task PutState([FromQuery]string stateId, [FromQuery]string activityId, [FromQuery]string agent, [FromQuery]Guid publishedResultID)
        {
            using (var ms = new MemoryStream(2048))
            {
                Request.Body.CopyTo(ms);
                var value = ms.ToArray();
                var agentObject = JsonConvert.DeserializeObject<Agent>(agent);
                //_xApiService.SaveState(value, stateId, activityId, agent);
                //_jobQueueService.EnqueueState(value, stateId, activityId, agent);
                await _repositoryService.SaveState(value, stateId, activityId, agentObject);
            }
        }

        [HttpGet("activities/state")]
        public async Task<string> GetState([FromQuery]string stateId, [FromQuery]string activityId, [FromQuery]string agent)
        {
            var agentObject = JsonConvert.DeserializeObject<Agent>(agent);
            return await _repositoryService.GetState(stateId, activityId, agentObject);
        }

        [ProducesResponseType(204)]
        [HttpPut("statements2")]
        public void PutStatement2([FromBody]Statement value, [FromQuery]Guid statementId, [FromQuery]Guid publishedResultID)
        {
            // var json = value.ToString();
            // _xApiService.SaveStatement(value, statementId);
            // var verb = value["verb"];
            // var verbId = verb["id"];
            // var statement = new Statement(value);
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
    }
}
