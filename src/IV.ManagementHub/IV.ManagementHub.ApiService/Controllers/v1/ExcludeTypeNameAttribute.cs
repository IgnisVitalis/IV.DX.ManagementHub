using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IV.ManagementHub.ApiService.Controllers.v1
{
    public sealed class ExcludeTypeNameAttribute : Attribute, IActionConstraintFactory, IOrderedFilter
    {
        public ExcludeTypeNameAttribute(string excluded) => Excluded = excluded;
        public string Excluded { get; }
        public int Order { get; set; } = 0;    
        public bool IsReusable => true;

        public IActionConstraint CreateInstance(IServiceProvider services)
            => new ExcludeTypeNameConstraint(Excluded);
    }
}
