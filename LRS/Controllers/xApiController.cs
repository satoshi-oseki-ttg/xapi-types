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

namespace bracken_lrs.Controllers
{
    [Route("tcapi")]
    [Authorize]
    public class xApiController : Controller
    {
        private readonly IxApiService _xApiService;
        private readonly IJobQueueService _jobQueueService;
        private readonly IRepositoryService _repositoryService;
        private static readonly Uri completed = new Uri("http://adlnet.gov/expapi/verbs/completed");

        private static readonly HttpClient httpClient = new HttpClient();

        public xApiController(IxApiService xApiService, IJobQueueService jobQueueService, IRepositoryService repositoryService)
        {
            _xApiService = xApiService;
            _jobQueueService = jobQueueService;
            _repositoryService = repositoryService;
        }

        [HttpGet("statements")]
        public async Task<IActionResult> GetStatement([FromQuery]Guid statementId, [FromQuery]Guid voidedStatementId)
        {
            Response.Headers.Add("X-Experience-API-Consistent-Through", DateTime.UtcNow.ToString("o"));

            var noParams = statementId == Guid.Empty && voidedStatementId == Guid.Empty;
            if (noParams)
            {
                return Ok(_repositoryService.GetStatements());
            }
            
            var id = voidedStatementId == Guid.Empty ? statementId : voidedStatementId;
            var statement = await _repositoryService.GetStatement(id, voidedStatementId != Guid.Empty);
            if (statement == null)
            {
                return NotFound();
            }

            return Ok(statement);
        }

        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [HttpPost("statements")]
        public async Task<IActionResult> PostStatement([FromBody]JObject obj)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                var userName = User.Identity.Name;
                var lrsUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";
                return Ok(await _repositoryService.SaveStatement(obj, null, lrsUrl, userName));
            }
            catch (Exception e)
            {
                return await Task.FromResult(BadRequest(e));
            }
        }



        // [ProducesResponseType(204)]
        // [HttpPut("statements")]
        // public async Task PutStatement([FromBody]Statement value, [FromQuery]Guid statementId, [FromQuery]Guid publishedResultID)
        // {
        //     var userName = User.Identity.Name;
        //     var lrsUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}";
        //     await _repositoryService.SaveStatement(value, null, lrsUrl, userName);
        //     // var json = value.ToString();
        //     // //_xApiService.SaveStatement(value, statementId);
        //     // _jobQueueService.EnqueueStatement(value);
        //     // var verb = value["verb"];
        //     // var verbId = verb["id"];
        //     // //var statement = new Statement(value);
        //     // //if (statement.verb.Id == completed)
        //     // if (verbId.Value<string>() == completed.ToString())
        //     // {
        //     //     var url = $"http://live.brackenlearning.com.satoshi.work/statements?statementId={statementId}&publishedResultID={publishedResultID}";
        //     //     var content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        //     //     httpClient.DefaultRequestHeaders.Authorization =
        //     //         new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
        //     //             System.Text.ASCIIEncoding.ASCII.GetBytes("bracken:welcome123")));
        //     //     httpClient.PutAsync(url, content);
        //     // }
        // }

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
