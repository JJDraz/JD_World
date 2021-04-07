using JD_Hateoas.Models;
using JD_Hateoas.Etag;
using JD_Hateoas.Form;
using JD_Hateoas.Helpers;
using JD_Hateoas.Search;
using JD_Hateoas.Sort;
using Newtonsoft.Json;

namespace JDWorldAPI.Models
{
    public class WorldRest : Resource, IEtaggable
    {
        [SearchableString]
        public string WorldName { get; set; }

        [SearchableString]
        public string TenantName { get; set; }

        public string ServerIP { get; set; }

        public string VoiceIP { get; set; }

        public Form Assign { get; set; }

        public string GetEtag()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return Md5Hash.ForString(serialized);
        }
    }
}
