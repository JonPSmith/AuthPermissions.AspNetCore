// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthPermissions.AspNetCore.HostedServices
{
    public class SetupDatabaseOnStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SetupDatabaseOnStartup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AuthPermissionsDbContext>();
                var options = services.GetRequiredService<IAuthPermissionsOptions>();
                try
                {
                    if (options.DatabaseType == AuthPermissionsOptions.DatabaseTypes.SqliteInMemory)
                        throw new AuthPermissionsException(
                            $"The in-memory database is created with in the {nameof(AuthPermissions.SetupExtensions)}");
                    else
                        await context.Database.MigrateAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<SetupDatabaseOnStartup>>();
                    logger.LogError(ex, "An error occurred while creating/migrating the SQL database.");

                    throw;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}