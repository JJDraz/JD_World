using Microsoft.AspNetCore.Http;

namespace JD_Hateoas.Etag
{ 
    public static class HttpRequestExtensions
    {
        public static IEtagHandlerFeature GetEtagHandler(this HttpRequest request)
            => request.HttpContext.Features.Get<IEtagHandlerFeature>();
    }
}
