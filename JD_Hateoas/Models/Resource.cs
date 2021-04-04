using Newtonsoft.Json;

namespace JD_Hateoas.Models
{
    public abstract class Resource : Link
    {
        [JsonIgnore]
        public Link Self { get; set; }
    }
}
