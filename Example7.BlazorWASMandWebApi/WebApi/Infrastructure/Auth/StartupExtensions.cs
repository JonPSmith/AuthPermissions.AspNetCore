using Example7.BlazorWASMandWebApi.Infrastructure.Auth.AuthP;
using Example7.BlazorWASMandWebApi.Infrastructure.Auth.Jwt;
using Example7.BlazorWASMandWebApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Auth;

public static class StartupExtensions
{
    internal static IServiceCollection AddAuth(this IServiceCollection services, ConfigurationManager config, IWebHostEnvironment webHostEnvironment)
    {
        // Must add identity before adding auth!
        services.AddIdentity();

        services.Configure<SecuritySettings>(config.GetSection(nameof(SecuritySettings)));
        if (config["SecuritySettings:Provider"].Equals("AzureAd", StringComparison.OrdinalIgnoreCase))
        {
            // TODO services.AddAzureAdAuth(config)
        }
        else
        {
            services.AddJwtAuth();
        }

        return services
                .AddAuthP(config, webHostEnvironment);
    }
}

