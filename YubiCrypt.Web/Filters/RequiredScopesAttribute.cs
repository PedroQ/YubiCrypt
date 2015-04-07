using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace YubiCrypt.Web.Filters
{
    public class RequiredScopesAttribute : AuthorizeAttribute
    {
        string[] _scopes;
        static string _scopeClaimType = "urn:oauth:scope";
        public static string ScopeClaimType
        {
            get { return _scopeClaimType; }
            set { _scopeClaimType = value; }
        }
        public RequiredScopesAttribute(params string[] scopes)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException("scopes");
            }
            _scopes = scopes;
        }
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var principal = actionContext.ControllerContext.RequestContext.Principal as ClaimsPrincipal;
            if (principal == null)
            {
                return false;
            }
            var grantedScopes = principal.FindAll(_scopeClaimType).Select(c => c.Value).ToList();
            foreach (var scope in _scopes)
            {
                if (grantedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            HttpResponseMessage response;
            if (actionContext.RequestContext.Principal.Identity.IsAuthenticated)
            {
                response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "insufficient_scope");
                response.Headers.Add("WWW-Authenticate", "Bearer error=\"insufficient_scope\"");
            }
            else
            {
                response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized");
            }
            actionContext.Response = response;
        }
    }
}