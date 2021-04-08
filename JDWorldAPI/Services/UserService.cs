using JDWorldAPI.Models;
using JD_Hateoas.Paging;
using JD_Hateoas.Search;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace JDWorldAPI.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<UserDto> _userManager;
        private readonly IMapper _mapper;

        public UserService(UserManager<UserDto> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<(bool Succeeded, string Error)> CreateUserAsync(RegisterForm form)
        {
            var entity = new UserDto
            {
                Email = form.Email,
                UserName = form.Email,
                FirstName = form.FirstName,
                LastName = form.LastName,
                TenantName = form.TenantName,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await _userManager.CreateAsync(entity, form.Password);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault()?.Description;
                return (false, firstError);
            }

            return (true, null);
        }

        public async Task<UserRest> GetUserAsync(ClaimsPrincipal user)
        {
            var entity = await _userManager.GetUserAsync(user);
            
            return _mapper.Map<UserRest>(entity);
        }

        public async Task<UserRest> GetUserByIdAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userManager.Users
                .SingleOrDefaultAsync(x => x.Id == userId, ct);

            return _mapper.Map<UserRest>(user);
        }

        public async Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal)
        {
            var user = await _userManager.GetUserAsync(principal);
            if (user == null) return null;

            return user.Id;
        }

        public async Task<PagedResults<UserRest>> GetUserCollectionAsync(
            PagingOptions pagingOptions,
            string tenantName,
            CancellationToken ct)
        {
            var Search = new string[1];
            Search[0] = "tenantName eq " + tenantName;

            var tenant = new SearchOptionsProcessor<UserRest, UserDto>(Search);
            
            IQueryable<UserDto> query = _userManager.Users;
            query = tenant.Apply(query);

            var size = await query.CountAsync(ct);

            var items = await query
                .Skip(pagingOptions.Offset.Value)
                .Take(pagingOptions.Limit.Value)
                .ProjectTo<UserRest>(_mapper.ConfigurationProvider)
                .ToArrayAsync(ct);

            return new PagedResults<UserRest>
            {
                Items = items,
                TotalSize = size
            };
        }
    }
}
