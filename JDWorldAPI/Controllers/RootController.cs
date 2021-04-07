using JDWorldAPI.Models;
using JD_Hateoas.Models;
using JD_Hateoas.Etag;
using JD_Hateoas.Form;
using Microsoft.AspNetCore.Mvc;

namespace JDWorldAPI.Controllers
{
    [Route("/")]
    [ApiVersion("2.0")]
    [ApiController]
    public class RootController : Controller
    {
        [HttpGet(Name = nameof(GetRoot))]
        [ResponseCache(CacheProfileName = "Static")]
        [Etag]
        public IActionResult GetRoot()
        {
            var response = new RootResponse
            {
                Self = Link.To(nameof(GetRoot)),
                Info = Link.To(nameof(InfoController.GetInfo)),
                Worlds = Link.ToCollection(nameof(WorldsController.GetWorldsAsync)),
                Users = Link.ToCollection(nameof(UsersController.GetVisibleUsersAsync)),
                Token = FormMetadata.FromModel(
                    new PasswordGrantForm(),
                    Link.ToForm(nameof(TokenController.TokenExchangeAsync),
                                null, relations: Form.Relation))
            };

            if (!Request.GetEtagHandler().NoneMatch(response))
            {
                return StatusCode(304, response);
            }

            return Ok(response);
        }
    }
}
