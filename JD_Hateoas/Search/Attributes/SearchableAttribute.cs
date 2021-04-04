using System;

namespace JD_Hateoas.Search
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SearchableAttribute : Attribute
    {
        public string EntityProperty { get; set; }

        public ISearchExpressionProvider ExpressionProvider { get; set; }
            = new DefaultSearchExpressionProvider();
    }
}
