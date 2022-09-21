using Ardalis.Specification;
using Example7.BlazorWASMandWebApi.Application;
using Example7.BlazorWASMandWebApi.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Persistence;

public static class StartupExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Add Repositories
        services.AddScoped(typeof(IRepository<>), typeof(RetailDbRepository<>));

        foreach (Type? aggregateRootType in
            typeof(IAggregateRoot).Assembly.GetExportedTypes()
                .Where(t => typeof(IAggregateRoot).IsAssignableFrom(t) && t.IsClass)
                .ToList())
        {
            // Add ReadRepositories.
            services.AddScoped(typeof(IReadRepository<>).MakeGenericType(aggregateRootType), sp =>
                sp.GetRequiredService(typeof(IRepository<>).MakeGenericType(aggregateRootType)));

        }

        return services;
    }
}

