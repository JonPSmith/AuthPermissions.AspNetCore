// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.InternalStartupServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.HostedServices
{
    /// This takes a comma delimited string of demo users from the "DemoUsers" in the appsettings.json file
    /// and adds each if they aren't in the individual account user
    /// NOTE: The email is also the password, so make sure the email is a valid password
    public class HostedIndividualAccountsAddDemoUsers : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public HostedIndividualAccountsAddDemoUsers(IServiceProvider serviceProvider)
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
            var scopedServices = scope.ServiceProvider;

            var startupService = new StartupIndividualAccountsAddDemoUsers();
            await startupService.StartupServiceAsync(scopedServices);
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}