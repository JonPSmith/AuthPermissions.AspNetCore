using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunMethodsSequentially;
using System;
using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.StartupServices
{
    public class StartupServiceMigrateAuthPDatabase : IStartupServiceToRunSequentially
    {
        public int OrderNum { get; } //The order of this startup services is defined by the order it registered

        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {
            var context = scopedServices.GetRequiredService<AuthPermissionsDbContext>();
            var options = scopedServices.GetRequiredService<AuthPermissionsOptions>();
            try
            {
                if (options.InternalData.AuthPDatabaseType == AuthPDatabaseTypes.SqliteInMemory)
                    throw new AuthPermissionsException(
                        $"The in-memory database is created by the {nameof(AuthPermissions.SetupExtensions.UsingInMemoryDatabase)} extension method");
                else
                    await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                var logger = scopedServices.GetRequiredService<ILogger<StartupServiceMigrateAuthPDatabase>>();
                logger.LogError(ex, "An error occurred while creating/migrating the SQL database.");

                throw;
            }
        }
    }
}
