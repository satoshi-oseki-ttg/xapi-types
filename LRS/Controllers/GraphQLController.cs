using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web;

using bracken_lrs.GraphQL;
using bracken_lrs.GraphQL.Services;

namespace bracken_lrs.Controllers
{
    /// <summary>
    /// GraphQL endpoint
    /// </summary>
    [Route("/")]
    public class GraphQLController : Controller
    {
        private readonly ILrsQueryService _lrsQueryService;

        /// <summary>
        /// Inject LrsQueryService
        /// </summary>
        public GraphQLController(ILrsQueryService lrsQueryService)
        {
            _lrsQueryService = lrsQueryService;
        }

        /// <summary>
        /// GraphQL entry point
        /// </summary>'
        [HttpPost]
        [Route("graphql")]
        [ProducesResponseType(200, Type = typeof(ExecutionResult))]
        [ProducesResponseType(404)]
        // [Authorize]
        public async Task<IActionResult> PostAsync([FromBody]GraphQLQuery query)
        {
            var schema = new Schema
            {
                Query = new LrsQuery(_lrsQueryService)
            };

            var result = await new DocumentExecuter().ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query.Query;

            }).ConfigureAwait(false);

            if (result.Errors?.Count > 0)
            {
                return BadRequest();
            }

            return Ok(result);
        }
    }
}
