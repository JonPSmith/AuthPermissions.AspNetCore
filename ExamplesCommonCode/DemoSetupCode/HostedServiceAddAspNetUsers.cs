// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExamplesCommonCode.DemoSetupCode
{
    public class HostedServiceAddAspNetUsers : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public HostedServiceAddAspNetUsers(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var config = services.GetRequiredService<IConfiguration>();
                var demoUsers = config["DemoUsers"];

                if (!string.IsNullOrEmpty(demoUsers))
                {
                    foreach (var userEmail in demoUsers.Split(',').Select(x => x.Trim()))
                    {
                        //NOTE: The password is the same as the user's Email
                        await userManager.CheckAddNewUserAsync(userEmail, userEmail);
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}