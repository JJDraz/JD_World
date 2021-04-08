using JDWorldAPI.Models;
using JD_Hateoas.Paging;
using JD_Hateoas.Search;
using JD_Hateoas.Sort;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JDWorldAPI.Services
{
    public interface IResidentService
    {
        Task<ResidentRest> GetResidentAsync(Guid residentId, CancellationToken ct);

        Task<Guid> CreateResidentAsync(
            string worldName,
            string worldUserEmail,
            string worldUserRole,
            CancellationToken ct);

        Task DeleteResidentAsync(Guid residentId, CancellationToken ct);
		
		Task<PagedResults<ResidentRest>> GetResidentCollectionAsync(
            PagingOptions pagingOptions,
            string tenantName,
            CancellationToken ct);

        Task<ResidentRest> GetResidentForUserIdAsync(
            Guid residentId,
            Guid userId,
            CancellationToken ct);

        Task<bool> IsResidentWorldAdminAsync(
            string userEmail,
            string worldName,
            CancellationToken ct);

        Task<PagedResults<ResidentRest>> GetResidentCollectionForUserIdAsync(
            Guid userId,
            PagingOptions pagingOptions,
            CancellationToken ct);
    }
}
