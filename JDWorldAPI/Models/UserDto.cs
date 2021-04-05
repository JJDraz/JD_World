using Microsoft.AspNetCore.Identity;
using System;

namespace JDWorldAPI.Models
{
    public class UserDto : IdentityUser<Guid>
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string TenantName { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
