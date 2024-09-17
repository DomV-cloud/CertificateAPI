using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Attributes
{
    public class ApiKeyAttribute : ServiceFilterAttribute
    {
        public ApiKeyAttribute()
        : base(typeof(ApiKeyAuthFilter))
        {
        }
    }
}
