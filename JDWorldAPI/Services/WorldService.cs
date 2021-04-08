using JDWorldAPI.Models;
using JD_Hateoas.Paging;
using JD_Hateoas.Search;
using JD_Hateoas.Sort;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JDWorldAPI.Services
{
    public class WorldService : IWorldService
    {
        private readonly JDWorldAPIContext _context;
        private readonly IMapper _mapper;

        public WorldService(JDWorldAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<WorldRest> GetWorldAsync(Guid id, CancellationToken ct)
        {
            var entity = await _context.Worlds.SingleOrDefaultAsync(r => r.Id == id, ct);
            if (entity == null) return null;

            return _mapper.Map<WorldRest>(entity);
        }

        public async Task<PagedResults<WorldRest>> GetWorldCollectionAsync(
            PagingOptions pagingOptions,
            bool allWorlds,
            string userEmail,
            string tenantName,
            CancellationToken ct)
        {

            IQueryable<WorldDto> query;

            if (allWorlds)
            {
                var Search = new string[1];
                Search[0] = "tenantName eq " + tenantName;

                var tenant = new SearchOptionsProcessor<WorldRest, WorldDto>(Search);

                query = _context.Worlds;
                query = tenant.Apply(query);
            } else {

                var allResidents = await _context.Residents.ToArrayAsync(ct);
                var validWorlds = new List<string>();

                foreach (var resident in allResidents)
                {
                    bool worldCitizen = resident.WorldUserEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase);
                    if (worldCitizen)
                    {
                        validWorlds.Add(resident.WorldName);
                    }
                }
                query = _context.Worlds.Where(r => validWorlds.Any(s => r.WorldName.Equals(s)));
            }

            var size = await query.CountAsync(ct);

            var items = await query
                .Skip(pagingOptions.Offset.Value)
                .Take(pagingOptions.Limit.Value)
                .ProjectTo<WorldRest>(_mapper.ConfigurationProvider)
                .ToArrayAsync(ct);

            return new PagedResults<WorldRest>
            {
                Items = items,
                TotalSize = size
            };
        }
    }
}
