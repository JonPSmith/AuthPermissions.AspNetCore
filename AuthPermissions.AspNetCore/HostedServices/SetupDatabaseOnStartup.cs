// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
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
                try
                {
                    if (context.Database.IsInMemory())
                        await context.Database.EnsureCreatedAsync(cancellationToken);
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