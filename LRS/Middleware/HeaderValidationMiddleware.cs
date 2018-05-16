using System;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace bracken_lrs.Middleware
{
    public class HeaderValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public HeaderValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers["X-Experience-API-Version"] = "1.0.3";

            if (context.Request.Method == "POST" && context.Request.Query.Keys.Contains("method") // An alternate request doesn't always have xAPI version.
                || context.Request.Method == "GET" && context.Request.Path.Value.EndsWith("about")) // GET about doesn't need a valid X-Experience-API-Version.
            {
                await _next.Invoke(context);
                return;
            }

            string xApiVersion = context.Request.Headers["X-Experience-API-Version"];
            var versionOneRegex = new Regex("^1\\.0(\\.[0-9])*$");
            if (xApiVersion == null || !versionOneRegex.IsMatch(xApiVersion))
            {
                // context.Response.Clear();
                context.Response.StatusCode = 400; // Bad request
                await context.Response.WriteAsync("xAPI version must be 1.0.*.");
                return;
                //return await new Task();
            }
 
            await _next.Invoke(context);
        }
    }
}
