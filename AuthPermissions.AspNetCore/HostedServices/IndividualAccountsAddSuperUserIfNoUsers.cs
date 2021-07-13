// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.HostedServices.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AuthPermissions.AspNetCore.HostedServices
{
    /// <summary>
    /// This hosted service is designed to add a user to the Individual Accounts
    /// authentication provider  when the application is deployed with a new database.
    /// It takes the new user's email and password from the appsettings file
    ///
    /// It will NOT add a new user if there are already users in the Individual Accounts database.
    /// It does this to ensure there security loophole where a new user could be added later.
    /// </summary>
    public class IndividualAccountsAddSuperUserIfNoUsers : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public IndividualAccountsAddSuperUserIfNoUsers(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Add the 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;

                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                if (userManager.Users.Any())
                    //If there are any users already in the Individual Accounts it does not add a super user
                    //This is to ensure that other users can be added this way.
                    return;

                var superUserInfo = services.GetSuperUserConfigData();
                if (!string.IsNullOrEmpty(superUserInfo.email))
                    await CheckAddNewUserAsync(userManager, superUserInfo.email, superUserInfo.password);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// This will add a user with the given email if they don't all ready exist
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static async Task<IdentityUser> CheckAddNewUserAsync(UserManager<IdentityUser> userManager, string email, string password)
        {
            var user = new IdentityUser { UserName = email, Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errorDescriptions = string.Join("\n", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException(
                    $"Tried to add user {email}, but failed. Errors:\n {errorDescriptions}");
            }

            return user;
        }
    }
}