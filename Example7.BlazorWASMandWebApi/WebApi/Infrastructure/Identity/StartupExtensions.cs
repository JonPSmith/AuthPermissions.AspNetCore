using Example7.BlazorWASMandWebApi.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Identity;

public static class StartupExtensions
{
    internal static IServiceCollection AddIdentity(this IServiceCollection services) =>
        services
            .AddIdentityCore<IdentityUser>(options =>
                options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders()
        .Services;
}

