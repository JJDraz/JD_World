using Newtonsoft.Json;

namespace JD_Hateoas.Form
{
    public class FormFieldOption
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        public object Value { get; set; }
    }
}
