using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Example7.BlazorWASMandWebApi.Application;
using Example7.BlazorWASMandWebApi.Infrastructure.Auth;
using Example7.BlazorWASMandWebApi.Infrastructure.OpenApi;
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
            .AddApiVersioning()
            .AddAuth(config, webHostEnvironment)
            .AddOpenApiDocumentation(config)
            .AddPersistence(config)
            .AddRouting(options => options.LowercaseUrls = true)
            .AddServices();
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config) =>
        builder
            .UseStaticFiles()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseDownForMaintenance(TenantTypes.HierarchicalTenant)
            .UseOpenApiDocumentation(config);

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers().RequireAuthorization();
        return builder;
    }

    internal static IServiceCollection AddServices(this IServiceCollection services) =>
            services
                .AddServices(typeof(ITransientService), ServiceLifetime.Transient)
                .AddServices(typeof(IScopedService), ServiceLifetime.Scoped);

    internal static IServiceCollection AddServices(this IServiceCollection services, Type interfaceType, ServiceLifetime lifetime)
    {
        var interfaceTypes =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => interfaceType.IsAssignableFrom(t)
                            && t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Service = t.GetInterfaces().FirstOrDefault(),
                    Implementation = t
                })
                .Where(t => t.Service is not null
                            && interfaceType.IsAssignableFrom(t.Service));

        foreach (var type in interfaceTypes)
        {
            services.AddService(type.Service!, type.Implementation, lifetime);
        }

        return services;
    }

    internal static IServiceCollection AddService(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient(serviceType, implementationType),
            ServiceLifetime.Scoped => services.AddScoped(serviceType, implementationType),
            ServiceLifetime.Singleton => services.AddSingleton(serviceType, implementationType),
            _ => throw new ArgumentException("Invalid lifeTime", nameof(lifetime))
        };
}

