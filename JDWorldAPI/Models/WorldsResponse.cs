using JD_Hateoas.Models;
using JD_Hateoas.Form;
using JD_Hateoas.Paging;

namespace JDWorldAPI.Models
{
    public class WorldsResponse : PagedCollection<WorldRest>
    {
        public Form WorldsQuery { get; set; }
    }
}
