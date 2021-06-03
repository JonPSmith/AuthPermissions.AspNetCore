// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.HostedServices
{
    public class AddAuthRolesUserOnStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public AddAuthRolesUserOnStartup(IServiceProvider serviceProvider)
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
                var findUserIdService = services.GetService<IFindUserIdService>();

                var roleLoader = new BulkLoadRolesService(context);
                var status = roleLoader.AddRolesToDatabaseIfEmpty(authOptions.RolesPermissionsSetupText,
                    authOptions.EnumPermissionsType);
                if (status.IsValid)
                {
                    var userLoader = new BulkLoadUsersService(context, findUserIdService);
                    status = await userLoader.AddUsersRolesToDatabaseIfEmptyAsync(authOptions.UserRolesSetupData);
                }
                status.IfErrorsTurnToException();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}