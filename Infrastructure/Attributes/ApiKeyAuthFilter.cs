using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Attributes
{
    public class ApiKeyAuthFilter : IAuthorizationFilter
    {
        private const string APIKEYNAME = "ApiKey";
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var apiKeyValue = context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey);

            if (!apiKeyValue)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key nebyl nalezen."
                };
                return;
            }

            var appSettings = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = appSettings.GetSection(APIKEYNAME);

            if (!apiKey.Value.Equals(extractedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "Neplatný API Key."
                };
                return;
            }
        }
    }
}
