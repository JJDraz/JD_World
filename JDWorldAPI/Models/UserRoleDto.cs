using Microsoft.AspNetCore.Identity;
using System;

namespace JDWorldAPI.Models
{
    public class UserRoleDto : IdentityRole<Guid>
    {
        public UserRoleDto()
            : base()
        { }

        public UserRoleDto(string roleName)
            : base(roleName)
        { }
    }
}
