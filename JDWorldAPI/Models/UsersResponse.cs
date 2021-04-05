using JD_Hateoas.Models;
using JD_Hateoas.Form;
using JD_Hateoas.Paging;

namespace JDWorldAPI.Models
{
    public class UsersResponse : PagedCollection<UserRest>
    {
        public Form Register { get; set; }

        public Link Me { get; set; }
    }
}
