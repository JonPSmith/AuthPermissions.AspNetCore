// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace AuthPermissions.AspNetCore.StartupServices
{
    public class StartupServicesIndividualAccountsAddDemoUsers : IStartupServiceToRunSequentially
    {
        //This must be run after the migation of the IndividualAccounts database
        //But before the SuperUser is added
        public int OrderNum { get; } = -5; 

        /// <summary>
        /// This takes a comma delimited string of demo users from the "DemoUsers" in the appsettings.json file
        /// and adds each if they aren't in the individual account user
        /// NOTE: The email is also the password, so make sure the email is a valid password
        /// </summary>
        /// <param name="scopedServices">This should be a scoped service</param>
        /// <returns></returns>
        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {     
            var userManager = scopedServices.GetRequiredService<UserManager<IdentityUser>>();
            var config = scopedServices.GetRequiredService<IConfiguration>();
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

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}