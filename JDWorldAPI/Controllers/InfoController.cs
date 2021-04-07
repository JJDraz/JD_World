using JDWorldAPI.Models;
using JD_Hateoas.Models;
using JD_Hateoas.Etag;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JDWorldAPI.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class InfoController : Controller
    {
        private readonly JDInfoRest _jdInfo;

        public InfoController(IOptions<JDInfoRest> jdInfoAccessor)
        {
            _jdInfo = jdInfoAccessor.Value;
            _jdInfo.Self = Link.To(nameof(GetInfo));
        }

        [HttpGet(Name = nameof(GetInfo))]
        [ResponseCache(CacheProfileName = "Static")]
        [Etag]
        public IActionResult GetInfo()
        {
            if (!Request.GetEtagHandler().NoneMatch(_jdInfo))
            {
                return StatusCode(304, _jdInfo);
            }

            return Ok(_jdInfo);
        }
    }
}
