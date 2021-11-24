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
    /// <summary>
    /// This hosted service runs before ASP.NET Core goes live, and migrates the AuthP database
    /// NOTE: Only works on single instance of the ASP.NET Core application
    /// </summary>
    public class HostedSetupAuthDatabaseOnStartup : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public HostedSetupAuthDatabaseOnStartup(IServiceProvider serviceProvider)
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
            var scopedServices = scope.ServiceProvider;

            var starupService = new StartupMigrateAuthDatabase();
            await starupService.StartupServiceAsync(scopedServices);
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}