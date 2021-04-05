using JDWorldAPI.Models;
using JD_Hateoas.Paging;
using JD_Hateoas.Search;
using JD_Hateoas.Sort;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

namespace JDWorldAPI.Services
{
    public interface IUserService
    {
        Task<PagedResults<UserRest>> GetUserCollectionAsync(
            PagingOptions pagingOptions,
            string userName,
            CancellationToken ct);

        Task<(bool Succeeded, string Error)> CreateUserAsync(RegisterForm form);

        Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal);

        Task<UserRest> GetUserByIdAsync(Guid userId, CancellationToken ct);

        Task<UserRest> GetUserAsync(ClaimsPrincipal user);
    }
}
