using System;
using bracken_lrs.Extensions;
using bracken_lrs.Models.Admin;
using bracken_lrs.Services;
using Microsoft.AspNetCore.Mvc;

namespace bracken_lrs.Controllers
{
        [Route("admin")]
        public class AdminController : Controller
        {
            private readonly IRepositoryService _repositoryService;

            public AdminController(
                IRepositoryService repositoryService
            )
            {
                _repositoryService = repositoryService;
            }

            [HttpPost("user")]
            [ProducesResponseType(200)]
            public IActionResult RegisterUser([FromBody]UserViewModel user)
            {
                _repositoryService.SetDb("lrs-admin").RegisterUser(user);

                return Ok();
            }

            /// <summary>
            /// A simple endpoint to test server is running.
            /// </summary>
            /// <returns>string message</returns>
            [HttpGet("ping")]
            public IActionResult Pong()
            {
              string ds = DateTime.Now.ToLongDateString();
              string ts = DateTime.Now.ToLongTimeString();
              string msg = $"I am alive {ds} {ts}";
              return Ok(msg);
            }


            // [HttpPost("tenant")]
            // [ProducesResponseType(200)]
            // public IActionResult RegisterTenant([FromBody]TenantModel tenant)
            // {
            //     _repositoryService.SetDb("admin").RegisterTenant(tenant);

            //     return Ok();
            // }
            // [HttpGet("user")]
            // public IActionResult GetUser([FromQuery] email)
            // {
            //     return Ok();
            // }

            // [HttpGet("tenant")]
            // public IActionResult GetTenant([FromQuery] tenant)
            // {
            //     return Ok();
            // }
        }
}