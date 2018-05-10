using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace bracken_lrs.Attributes
{
    public class PostStatementActionAttribute : Attribute, IActionConstraint
    {
        public int Order => 999;
        public bool Accept(ActionConstraintContext context)
        {
            if (string.IsNullOrEmpty(context.RouteContext.HttpContext.Request.ContentType)
                && context.RouteContext.HttpContext.Request.Query.ContainsKey("method"))
            {
                return context.CurrentCandidate.Action.Parameters.Any(x => x.Name == "method");
            }

            return context.CurrentCandidate.Constraints.Any(x =>
            {
                var contentType = context.RouteContext.HttpContext.Request.ContentType;
                var consumes = x as ConsumesAttribute;
                var contentTypeMatch = consumes?.ContentTypes?.Any(c => contentType.Contains(c)) ?? false; // contentType can be "multipart/mixed; boundary=-------------417811395"
                Console.WriteLine($"{contentType}: {contentTypeMatch} : {context.CurrentCandidate.Action.DisplayName}");
                return contentTypeMatch;
            });
        }
    }
}
