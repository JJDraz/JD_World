using JD_Hateoas.Models;
using JD_Hateoas.Etag;
using JD_Hateoas.Helpers;
using Newtonsoft.Json;

namespace JDWorldAPI.Models
{
    public class JDInfoRest : Resource, IEtaggable
    {
        public string Title { get; set; }

        public string Tagline { get; set; }

        public string Email { get; set; }

        public string Website { get; set; }

        public Address Location { get; set; }

        public string GetEtag()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return Md5Hash.ForString(serialized);
        }
    }
}
