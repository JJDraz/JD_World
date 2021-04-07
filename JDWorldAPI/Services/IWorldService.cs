using JDWorldAPI.Models;
using JD_Hateoas.Paging;
using JD_Hateoas.Search;
using JD_Hateoas.Sort;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JDWorldAPI.Services
{
    public interface IWorldService
    {
        Task<WorldRest> GetWorldAsync(
            Guid id,
            CancellationToken ct);

        Task<PagedResults<WorldRest>> GetWorldCollectionAsync(
            PagingOptions pagingOptions,
            string tenantName,
            CancellationToken ct);
    }
}
