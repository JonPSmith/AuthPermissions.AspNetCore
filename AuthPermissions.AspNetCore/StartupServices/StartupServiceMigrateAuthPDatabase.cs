using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunMethodsSequentially;
using System;
using System.Threading.Tasks;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.AspNetCore.StartupServices
{
    /// <summary>
    /// Startup service that migrates the AuthP database
    /// </summary>
    public class StartupServiceMigrateAuthPDatabase : IStartupServiceToRunSequentially
    {
        /// <summary>
        /// Sets the order. Default is zero. If multiple classes have same OrderNum, then run in the order they were registered
        /// </summary>
        public int OrderNum { get; } = -5; //This runs after any other migration, but before any bulk load startup services

        /// <summary>
        /// This will migrate the AuthP database
        /// </summary>
        /// <param name="scopedServices"></param>
        /// <returns></returns>
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
