using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Text;
using System.Threading.Tasks;

namespace JD_Hateoas.Helpers
{
    public class IonOutputFormatter : TextOutputFormatter
    {
        private readonly NewtonsoftJsonOutputFormatter _jsonOutputFormatter;

        public IonOutputFormatter(NewtonsoftJsonOutputFormatter jsonOutputFormatter)
        {
            if (jsonOutputFormatter == null) throw new ArgumentNullException(nameof(jsonOutputFormatter));
            _jsonOutputFormatter = jsonOutputFormatter;

            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/ion+json"));
            SupportedEncodings.Add(Encoding.UTF8);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            => _jsonOutputFormatter.WriteResponseBodyAsync(context, selectedEncoding);
    }
}
