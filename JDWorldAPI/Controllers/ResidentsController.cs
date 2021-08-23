using JDWorldAPI.Models;
using JDWorldAPI.Services;
using JD_Hateoas.Models;
using JD_Hateoas.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JDWorldAPI.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class ResidentsController : Controller
    {
        private readonly IResidentService _residentService;
        private readonly IUserService _userService;
        private readonly IAuthorizationService _authzService;
        private readonly PagingOptions _defaultPagingOptions;

        public ResidentsController(
			IResidentService residentService,
            IUserService userService,
            IAuthorizationService authzService,
            IOptions<PagingOptions> defaultPagingOptionsAccessor)
			
        {
            _residentService = residentService;
            _userService = userService;
            _authzService = authzService;
            _defaultPagingOptions = defaultPagingOptionsAccessor.Value;
        }

        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet(Name = nameof(GetVisibleResidents))]
        public async Task<IActionResult> GetVisibleResidents(
            [FromQuery] PagingOptions pagingOptions,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));

            pagingOptions.Offset = pagingOptions.Offset ?? _defaultPagingOptions.Offset;
            pagingOptions.Limit = pagingOptions.Limit ?? _defaultPagingOptions.Limit;

            var residents = new PagedResults<ResidentRest>();

            var user = await _userService.GetUserAsync(User);

            var userCanSeeAllResidents = await _authzService.AuthorizeAsync(User, "ViewAllResidentsPolicy");
            if (userCanSeeAllResidents.Succeeded)
            {
                residents = await _residentService.GetResidentCollectionAsync(
                    pagingOptions, user.TenantName, ct);
            }
            else
            {
                var userId = await _userService.GetUserIdAsync(User);
                if (userId != null)
                {
                    residents = await _residentService.GetResidentCollectionForUserIdAsync(
                        userId.Value, pagingOptions, ct);
                }
            }

            var collectionLink = Link.ToCollection(nameof(GetVisibleResidents));
            var collection = PagedCollection<ResidentRest>.Create(
                collectionLink,
                residents.Items.ToArray(),
                residents.TotalSize,
                pagingOptions);

            return Ok(collection);
        }

        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("{residentId}", Name = nameof(GetResidentByIdAsync))]
        public async Task<IActionResult> GetResidentByIdAsync(
            Guid residentId,
            CancellationToken ct)
        {
            var user = await _userService.GetUserAsync(User);
            if (user == null) return NotFound();

            var resident = await _residentService.GetResidentAsync(residentId, ct);
            if (resident == null) return NotFound();

            var userCanSeeAllResidents = await _authzService.AuthorizeAsync(User, "ViewAllResidentsPolicy");
            if (!userCanSeeAllResidents.Succeeded)
            {
                // Ooops missed WorldAdmin
                var canViewInWorld = await _residentService.IsResidentWorldAdminAsync(user.Email, resident.WorldName, ct);
                if ((!canViewInWorld) && (!resident.WorldUserEmail.Equals(user.Email)))
                {
                    return Unauthorized();
                }
            }
            return Ok(resident);
        }

        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpDelete("{residentId}", Name = nameof(DeleteResidentByIdAsync))]
        public async Task<IActionResult> DeleteResidentByIdAsync(
            Guid residentId,
            CancellationToken ct)
        {
            var user = await _userService.GetUserAsync(User);
            if (user == null) return NotFound();
            var userId = await _userService.GetUserIdAsync(User);

            var resident = await _residentService.GetResidentForUserIdAsync(residentId, userId.Value, ct);
            if (resident != null)
            {
                await _residentService.DeleteResidentAsync(residentId, ct);
                return NoContent();
            }

            resident = await _residentService.GetResidentAsync(residentId, ct);
            if (resident == null) return NotFound();

            var userCanSeeAllResidents = await _authzService.AuthorizeAsync(User, "ViewAllResidentsPolicy");
            if (!userCanSeeAllResidents.Succeeded)
            {
                // Ooops. Missed checking for WorldAdmin.
                var canDeleteInWorld = await _residentService.IsResidentWorldAdminAsync(user.Email, resident.WorldName, ct);
                if (!canDeleteInWorld)
                {
                    return Unauthorized();
                }
            }

            await _residentService.DeleteResidentAsync(residentId, ct);
            return NoContent();
        }


        // Ooops. Missed a patch sample.
        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpPatch("{residentId}", Name = nameof(ToggleResidentRoleByIdAsync))]
        public async Task<IActionResult> ToggleResidentRoleByIdAsync(
            Guid residentId,
            CancellationToken ct)
        {
            var user = await _userService.GetUserAsync(User);
            if (user == null) return NotFound();

            var resident = await _residentService.GetResidentAsync(residentId, ct);
            if (resident == null) return NotFound();

            var userCanSeeAllResidents = await _authzService.AuthorizeAsync(User, "ViewAllResidentsPolicy");
            if (!userCanSeeAllResidents.Succeeded)
            {
                var canUpdateInWorld = await _residentService.IsResidentWorldAdminAsync(user.Email, resident.WorldName, ct);
                if (!canUpdateInWorld)
                {
                    return Unauthorized();
                }
            }

            string newRole;
            if (resident.WorldRole.Equals("WorldAdmin"))
            {
                newRole = "Citizen";
            } else
            {
                newRole = "WorldAdmin";
            }

            await _residentService.UpdateResidentRoleAsync(residentId, newRole, ct);
            return NoContent();
        }
    }
}
