// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.HostedServices
{
    /// This hosted service will add a single user to ASP.NET Core individual accounts identity system using data in the appsettings.json file.
    /// This is here to allow you add a super-admin user when you first start up the application on a new system
    /// NOTE: for security reasons this will only add a new user if there aren't any users already in the individual accounts database
    public class AddRolesTenantsUsersIfEmptyOnStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public AddRolesTenantsUsersIfEmptyOnStartup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Run on startup
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<AuthPermissionsDbContext>();
            var authOptions = services.GetRequiredService<AuthPermissionsOptions>();
            var findUserIdServiceFactory = services.GetRequiredService<IAuthPServiceFactory<IFindUserInfoService>>();

            var status = await context.SeedRolesTenantsUsersIfEmpty(authOptions, findUserIdServiceFactory);
            status.IfErrorsTurnToException();
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}