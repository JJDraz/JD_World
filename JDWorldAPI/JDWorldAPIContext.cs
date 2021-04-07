using JDWorldAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace JDWorldAPI
{
    public class JDWorldAPIContext : IdentityDbContext<UserDto, UserRoleDto, Guid>
    {
        public JDWorldAPIContext(DbContextOptions options)
            : base(options) { }

        public DbSet<WorldDto> Worlds { get; set; }

    }
}
