using System;

namespace JD_Hateoas.Search
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SearchableStringAttribute : SearchableAttribute
    {
        public SearchableStringAttribute()
        {
            ExpressionProvider = new StringSearchExpressionProvider();
        }
    }
}
