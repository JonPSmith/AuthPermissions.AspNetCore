// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.InternalStartupServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.HostedServices
{
    /// <summary>
    /// This hosted service is designed to add a user to the Individual Accounts
    /// authentication provider when the application is deployed with a new database.
    /// It takes the new user's email and password from the appsettings file
    /// </summary>
    public class HostedIndividualAccountsAddSuperUser<TIdentityUser> : IHostedService
        where TIdentityUser : IdentityUser, new()
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public HostedIndividualAccountsAddSuperUser(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// This will ensure that a user whos email/password is held in the "SuperAdmin" section of 
        /// the appsettings.json file is in the individual account athentication database
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            var service = new StartupIndividualAccountsAddSuperUser<TIdentityUser>();
            await service.StartupServiceAsync(scopedServices);
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}