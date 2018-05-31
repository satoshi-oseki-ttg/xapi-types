using System;
using System.Linq;
using bracken_lrs.Extensions;
using bracken_lrs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;

namespace bracken_lrs.Attributes
{
    // Get tenant name from hostname and set it to RouteData so that xAPIController can
    // switch db to the correspoinding one.
    // nzrugby.lrs.brackenlearning.com => tenant: nzrugby
    public class TenantAttribute : ActionFilterAttribute
    {
        private readonly IRepositoryService _repositoryService;

        public TenantAttribute(IRepositoryService repositoryService)
        {
            _repositoryService = repositoryService;
        }
        
        public override void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            var fullAddress = actionExecutingContext.HttpContext?.Request?
                .Headers?["Host"].ToString()?.Split('.');
            if (fullAddress.Length < 4 || fullAddress[1] != "lrs")
            {
                actionExecutingContext.Result = new StatusCodeResult(404);
                base.OnActionExecuting(actionExecutingContext);
            }
            else
            {
                var subdomain = fullAddress[0];
                var tenant = _repositoryService.SetDb("lrs-admin").GetTenantBySubdomain(subdomain);

                if (tenant != null)
                {
                    actionExecutingContext.RouteData.Values.Add("tenant", tenant.Name.ToLower());
                    base.OnActionExecuting(actionExecutingContext);
                }
                else
                {
                    actionExecutingContext.Result = new StatusCodeResult(404);
                    base.OnActionExecuting(actionExecutingContext);
                }
            }
        }
    }
}
