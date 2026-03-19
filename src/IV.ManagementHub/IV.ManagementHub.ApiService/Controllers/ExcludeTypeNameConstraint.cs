using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace IV.ManagementHub.ApiService.Controllers
{
    public sealed class ExcludeTypeNameConstraint : IActionConstraint
    {
        private readonly string _excluded;
        public int Order => 0;

        public ExcludeTypeNameConstraint(string excluded)
            => _excluded = excluded.ToLowerInvariant();

        public bool Accept(ActionConstraintContext context)
        {
            var routeValue = context.RouteContext.RouteData.Values["typeName"]?
                             .ToString()?.ToLowerInvariant();
            return routeValue != _excluded;
        }
    }
}