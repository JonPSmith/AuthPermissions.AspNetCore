// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.HostedServices
{
    public class AddTenantsOnStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public AddTenantsOnStartup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AuthPermissionsDbContext>();
                var authOptions = services.GetRequiredService<IAuthPermissionsOptions>();

                var service = new BulkLoadTenantsService(context);
                var status =
                    await service.AddTenantsToDatabaseAsync(authOptions.UserTenantSetupText, authOptions);

                status.IfErrorsTurnToException();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}