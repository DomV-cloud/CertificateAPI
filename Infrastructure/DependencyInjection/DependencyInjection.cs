using Infrastructure.Attributes;
using Application.Interfaces.Certificates.CertificateAuthorityService;
using Infrastructure.Services.Certificates;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.Services.CSR;
using Application.Interfaces.Repository.Certificates;
using Infrastructure.Repository.Certificates;
using Application.Interfaces.Logging;
using Infrastructure.Services.Logging;

namespace Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
          this IServiceCollection services)
        {
            services.AddScoped<ICsrService, CsrService>();
            services.AddScoped<ICertificateAuthorityService, CertificateAuthorityService>();
            services.AddScoped<ICertificateRepository, CertificatesRepository>();
            services.AddScoped<ILoggingService, LoggingService>();

            services.AddScoped<ApiKeyAuthFilter>();

            return services;
        }
    }
}
