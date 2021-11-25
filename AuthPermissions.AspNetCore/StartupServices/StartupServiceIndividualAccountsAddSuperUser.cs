// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace AuthPermissions.AspNetCore.StartupServices
{
    /// <summary>
    /// This is a complex method that can handle a individual account user with a 
    /// personalized IdentityUser type
    /// </summary>
    public class StartupServiceIndividualAccountsAddSuperUser<TIdentityUser> : IStartupServiceToRunSequentially
        where TIdentityUser : IdentityUser, new()
    {
        //These must be after migrations, after adding demo users and before AuthP bulk load is run
        public int OrderNum { get; } = -1; //These must be after migrations, but after adding demo users. 

        /// <summary>
        /// This will ensure that a user whos email/password is held in the "SuperAdmin" section of 
        /// the appsettings.json file is in the individual account athentication database
        /// </summary>
        /// <param name="scopedServices">This should be a scoped service</param>
        /// <returns></returns>
        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {
            var userManager = scopedServices.GetRequiredService<UserManager<TIdentityUser>>();

            var (email, password) = scopedServices.GetSuperUserConfigData();
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
