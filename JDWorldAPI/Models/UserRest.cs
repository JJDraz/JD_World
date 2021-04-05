using JD_Hateoas.Models;
using JD_Hateoas.Search;
using System;

namespace JDWorldAPI.Models
{
    public class UserRest : Resource
    {
        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [SearchableString]
        public string TenantName { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
