// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthPermissions.AspNetCore.HostedServices
{
    /// <summary>
    /// This will run before ASP.NET Core goes live, and migrates 
    /// </summary>
    public class SetupAuthDatabaseOnStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SetupAuthDatabaseOnStartup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// This will migrate the AuthP's database on startup
        /// NOTE: Only works on single instance of the ASP.NET Core application
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<AuthPermissionsDbContext>();
            var options = services.GetRequiredService<AuthPermissionsOptions>();
            try
            {
                if (options.InternalData.AuthPDatabaseType == AuthPDatabaseTypes.SqliteInMemory)
                    throw new AuthPermissionsException(
                        $"The in-memory database is created by the {nameof(AuthPermissions.SetupExtensions.UsingInMemoryDatabase)} extension method");
                else
                    await context.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<SetupAuthDatabaseOnStartup>>();
                logger.LogError(ex, "An error occurred while creating/migrating the SQL database.");

                throw;
            }
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}