using JDWorldAPI.Models;
using JD_Hateoas.Paging;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JDWorldAPI.Services
{
    public class ResidentService : IResidentService
    {
        private readonly JDWorldAPIContext _context;
        private readonly UserManager<UserDto> _userManager;
        private readonly IMapper _mapper;

        public ResidentService(
            JDWorldAPIContext context,
			UserManager<UserDto> userManager,
            IMapper mapper)
        {
            _context = context;
			_userManager = userManager;
            _mapper = mapper;
        }

        public async Task<Guid> CreateResidentAsync(
            string worldName,
            string worldUserEmail,
            string worldUserRole,
            CancellationToken ct)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(c => c.Email == worldUserEmail, ct);
            if (user == null) throw new ArgumentException("Email is not registered.");

            var world = await _context.Worlds
                .SingleOrDefaultAsync(r => r.WorldName == worldName, ct);
            if (world == null) throw new ArgumentException("Invalid world id.");

            var id = Guid.NewGuid();

            var newResident = _context.Residents.Add(new ResidentDto
            {
                Id = id,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                WorldRole = worldUserRole,
                WorldName = worldName,
				WorldUserEmail = worldUserEmail
            });

            var created = await _context.SaveChangesAsync(ct);
            if (created < 1) throw new InvalidOperationException("Could not create the resident.");

            return id;
        }

        public async Task DeleteResidentAsync(Guid residentId, CancellationToken ct)
        {
            var resident = await _context.Residents
                .SingleOrDefaultAsync(b => b.Id == residentId, ct);
            if (resident == null) return;

            _context.Residents.Remove(resident);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<ResidentRest> GetResidentAsync(
            Guid residentId,
            CancellationToken ct)
        {
            var entity = await _context.Residents
                .SingleOrDefaultAsync(b => b.Id == residentId, ct);

            if (entity == null) return null;

            return _mapper.Map<ResidentRest>(entity);
        }

        public async Task<ResidentRest> GetResidentForUserIdAsync(
            Guid residentId,
            Guid userId,
            CancellationToken ct)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(c => c.Id == userId, ct);

            var entity = await _context.Residents
                .SingleOrDefaultAsync(b => b.Id == residentId && b.WorldUserEmail == user.Email, ct);

            if (entity == null) return null;

            return _mapper.Map<ResidentRest>(entity);
        }

        public async Task<bool> IsResidentWorldAdminAsync(
            string userEmail,
            string worldName,
            CancellationToken ct)
        {
            var entity = await _context.Residents
                .SingleOrDefaultAsync(b => b.WorldUserEmail == userEmail 
                                        && b.WorldName == worldName 
                                        && b.WorldRole == "WorldAdmin", ct);

            if (entity == null) return false;
            else return true;
        }

        public async Task<PagedResults<ResidentRest>> GetResidentCollectionAsync(
            PagingOptions pagingOptions,
            string tenantName,
            CancellationToken ct)
        {
            var tenantWorlds = await _context.Worlds.Where(r => r.TenantName == tenantName).ToArrayAsync(ct);
            var validWorlds = new List<string>();

            foreach (var world in tenantWorlds)
            {
                validWorlds.Add(world.WorldName);
            }
            IQueryable<ResidentDto> query = _context.Residents.Where(r => validWorlds.Any(s => r.WorldName.Equals(s)));

            var size = await query.CountAsync(ct);

            var items = await query
                .Skip(pagingOptions.Offset.Value)
                .Take(pagingOptions.Limit.Value)
                .ProjectTo<ResidentRest>(_mapper.ConfigurationProvider)
                .ToArrayAsync(ct);

            return new PagedResults<ResidentRest>
            {
                Items = items,
                TotalSize = size
            };
        }

        public async Task<PagedResults<ResidentRest>> GetResidentCollectionForUserIdAsync(
            Guid userId,
            PagingOptions pagingOptions,
            CancellationToken ct)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(c => c.Id == userId, ct);

            IQueryable<ResidentDto> query = _context.Residents
                .Where(b => b.WorldUserEmail == user.Email);

            var size = await query.CountAsync(ct);

            var items = await query
                .Skip(pagingOptions.Offset.Value)
                .Take(pagingOptions.Limit.Value)
                .ProjectTo<ResidentRest>(_mapper.ConfigurationProvider)
                .ToArrayAsync(ct);

            return new PagedResults<ResidentRest>
            {
                Items = items,
                TotalSize = size
            };
        }

        // Ooops forgot Update
        public async Task UpdateResidentRoleAsync(
            Guid residentId,
            string worldUserRole,
            CancellationToken ct)
        {
            var resident = await _context.Residents
                .SingleOrDefaultAsync(b => b.Id == residentId, ct);
            if (resident == null) throw new ArgumentException("Invalid resident id."); ;

            resident.ModifiedAt = DateTimeOffset.UtcNow;
            resident.WorldRole = worldUserRole;

            var updated = await _context.SaveChangesAsync(ct);
            if (updated < 1) throw new InvalidOperationException("Could not update the resident.");

        }

    }
}
