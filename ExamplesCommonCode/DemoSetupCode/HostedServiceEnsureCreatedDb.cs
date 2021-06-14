// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamplesCommonCode.DemoSetupCode
{
    public class HostedServiceEnsureCreatedDb<TContext> : IHostedService where TContext : DbContext 
    {
        private readonly IServiceProvider _serviceProvider;

        public HostedServiceEnsureCreatedDb(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<TContext>();

                if (context.Database.IsInMemory())
                    await context.Database.EnsureCreatedAsync(cancellationToken);
                else
                    await context.Database.MigrateAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}