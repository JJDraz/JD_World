using JD_Hateoas.Models;
using JD_Hateoas.Search;
using JD_Hateoas.Sort;
using System;

namespace JDWorldAPI.Models
{
    public class ResidentRest : Resource
    {
        public string WorldName { get; set; }
        public string WorldUserEmail { get; set; }
        public string WorldRole { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Link Cancel { get; set; }

    }
}
