// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore.InternalStartupServices
{
    /// <summary>
    /// This is a complex method that can handle a individual account user with a 
    /// personalized IdentityUser type
    /// </summary>
    internal class StartupIndividualAccountsAddSuperUser<TIdentityUser> 
        where TIdentityUser : IdentityUser, new()
    {
        /// <summary>
        /// This will ensure that a user whos email/password is held in the "SuperAdmin" section of 
        /// the appsettings.json file is in the individual account athentication database
        /// </summary>
        /// <param name="scopedService">This should be a scoped service</param>
        /// <returns></returns>
        public async ValueTask StartupServiceAsync(IServiceProvider scopedService)
        {
            var userManager = scopedService.GetRequiredService<UserManager<TIdentityUser>>();

            var (email, password) = scopedService.GetSuperUserConfigData();
            if (!string.IsNullOrEmpty(email))
                await CheckAddNewUserAsync(userManager, email, password);
        }

        /// <summary>
        /// This will add a user with the given email if they don't all ready exist
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static async Task CheckAddNewUserAsync(UserManager<TIdentityUser> userManager, string email, string password)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
                return;

            user = new TIdentityUser { UserName = email, Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errorDescriptions = string.Join("\n", result.Errors.Select(x => x.Description));
                throw new InvalidOperationException(
                    $"Tried to add user {email}, but failed. Errors:\n {errorDescriptions}");
            }
        }
    }
}
