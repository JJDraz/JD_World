using JD_Hateoas.Models;
using JD_Hateoas.Etag;
using JD_Hateoas.Form;
using JD_Hateoas.Helpers;
using Newtonsoft.Json;

namespace JDWorldAPI.Models
{
    public class RootResponse : Resource, IEtaggable
    {
        public Link Info { get; set; }

        public Link Worlds { get; set; }

        public Link Users { get; set; }

        public Form Token { get; set; }

        public string GetEtag()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return Md5Hash.ForString(serialized);
        }
    }
}
