using JDWorldAPI.Models;
using JDWorldAPI.Services;
using JD_Hateoas.Models;
using JD_Hateoas.Etag;
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
    [ApiVersion("3.0")]
    [Route("/api/[controller]")]
    [ApiController]
    public class WorldsController : Controller
    {
        private readonly IWorldService _worldService;
        private readonly IResidentService _residentService;
        private readonly IUserService _userService;
        private readonly IAuthorizationService _authzService;
        private readonly PagingOptions _defaultPagingOptions;

        public WorldsController(
            IWorldService worldService,
            IResidentService residentService,
            IUserService userService,
            IAuthorizationService authzService,
            IOptions<PagingOptions> defaultPagingOptionsAccessor)
        {
            _worldService = worldService;
            _residentService = residentService;
            _userService = userService;
            _authzService = authzService;
            _defaultPagingOptions = defaultPagingOptionsAccessor.Value;
        }

        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet(Name = nameof(GetWorldsAsync))]
        public async Task<IActionResult> GetWorldsAsync(
            [FromQuery] PagingOptions pagingOptions,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));

            pagingOptions.Offset = pagingOptions.Offset ?? _defaultPagingOptions.Offset;
            pagingOptions.Limit = pagingOptions.Limit ?? _defaultPagingOptions.Limit;

            var canViewAllWorlds = await _authzService.AuthorizeAsync(User, "ViewAllWorldsPolicy");
            bool allWorlds;

            if (canViewAllWorlds.Succeeded) allWorlds = true;
            else allWorlds = false;
            
            var user = await _userService.GetUserAsync(User);
            var worlds = await _worldService.GetWorldCollectionAsync(
                pagingOptions,
                allWorlds,
                user.Email,
                user.TenantName,
                ct);

            var collection = PagedCollection<WorldRest>.Create<WorldsResponse>(
                Link.ToCollection(nameof(GetWorldsAsync)),
                worlds.Items.ToArray(),
                worlds.TotalSize,
                pagingOptions);

            return Ok(collection);
        }

        // GET /worlds/{worldId}
        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("{worldId}", Name = nameof(GetWorldByIdAsync))]
        [ResponseCache(CacheProfileName = "Resource")]
        [Etag]
        public async Task<IActionResult> GetWorldByIdAsync(Guid worldId, CancellationToken ct)
        {
            var world = await _worldService.GetWorldAsync(worldId, ct);
            if (world == null) return NotFound();

            if (!Request.GetEtagHandler().NoneMatch(world))
            {
                return StatusCode(304, world);
            }

            return Ok(world);
        }


        // POST /worlds/{worldId}/residents
        [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [HttpPost("{worldId}/residents", Name = nameof(CreateResidentForWorldAsync))]
        public async Task<IActionResult> CreateResidentForWorldAsync(
            Guid worldId,
            [FromBody] ResidentForm form,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError(ModelState));

            var user = await _userService.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var world = await _worldService.GetWorldAsync(worldId, ct);
            if (world == null) return NotFound();

            var canRegisterUser = await _authzService.AuthorizeAsync(User, "RegisterUsersPolicy");

            if (!canRegisterUser.Succeeded)
            {
                var canRegisterInWorld = await _residentService.IsResidentWorldAdminAsync(user.Email, world.WorldName, ct);
                if (!canRegisterInWorld)
                {
                    return Unauthorized();
                }
            }

            var residentId = await _residentService.CreateResidentAsync(
               world.WorldName, form.WorldUserRole, form.WorldUserEmail, ct);

            return Created(
                Url.Link(nameof(ResidentsController.GetResidentByIdAsync),
                new { residentId }),
                null);
        }

    }
}
