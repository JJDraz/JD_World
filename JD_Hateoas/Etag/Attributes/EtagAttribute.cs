using JD_Hateoas.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace JD_Hateoas.Etag
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EtagAttribute : Attribute, IFilterFactory
    {
        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
            => new EtagHeaderFilter();
    }
}
