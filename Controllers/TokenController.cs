using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MapIndex.Controllers
{
    public class TokenController : Controller
    {
        private Services.AuthenticationService iAuth =
      new Services.AuthenticationService(new Data.Authentication.IAuthCacheRepository(new Data.Authentication.AuthRepository(),
          new Infrastructure.Caching.MemoryCacheProvider()));
       

        public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                // We'd normally just use "*" for the allow-origin header, 
                // but Chrome (and perhaps others) won't allow you to use authentication if
                // the header is set to "*".
                // TODO: Check elsewhere to see if the origin is actually on the list of trusted domains.
                var ctx = filterContext.RequestContext.HttpContext;
                var origin = ctx.Request.Headers["Origin"];
                var allowOrigin = !string.IsNullOrWhiteSpace(origin) ? origin : "*";
                ctx.Response.AddHeader("Access-Control-Allow-Origin", allowOrigin);
                ctx.Response.AddHeader("Access-Control-Allow-Headers", "*");
                ctx.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                base.OnActionExecuting(filterContext);
            }
        }
        [AllowCrossSiteJson]
        [HttpPost]
        public ActionResult FetchTokenData(Models.Authentication.ViewModels.AuthViewModel auth)
        {
       
            var response = new
            {
                Validates = iAuth.IsUserValid(auth),
                ticket = RandomGenerators.RandomGenerator.GenerateRandomWord(26),
                token = RandomGenerators.RandomGenerator.GenerateRandomWord(25)
            };

            return Json(response);

        }

    }
}