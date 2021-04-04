using System;

namespace JD_Hateoas.Search
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SearchableDateTimeAttribute : SearchableAttribute
    {
        public SearchableDateTimeAttribute()
        {
            ExpressionProvider = new DateTimeSearchExpressionProvider();
        }
    }
}
