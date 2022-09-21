using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Example7.BlazorWASMandWebApi.Infrastructure.Auth;
using Example7.BlazorWASMandWebApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Example7.BlazorWASMandWebApi.Infrastructure;

public static class StartupExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, ConfigurationManager config, IWebHostEnvironment webHostEnvironment)
    {
        return services
            .AddAuth(config, webHostEnvironment)
            // .AddOpenApiDocumentation(config)
            .AddRepositories()
            .AddRouting(options => options.LowercaseUrls = true);
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config) =>
        builder
            .UseStaticFiles()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseDownForMaintenance(TenantTypes.HierarchicalTenant);
            // .UseOpenApiDocumentation(config);

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers().RequireAuthorization();
        return builder;
    }
}

