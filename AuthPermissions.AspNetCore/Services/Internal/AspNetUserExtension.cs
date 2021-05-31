// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore.Services.Internal
{
    internal static class AspNetUserExtension
    {
        /// <summary>
        /// This adds a user using the email/password in the "SuperAdmin" section of the appsettings.json file
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static async Task CheckAddSuperAdminAsync(this IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;

                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                var config = services.GetRequiredService<IConfiguration>();
                var superSection = config.GetSection("SuperAdmin");
                if (superSection == null)
                    return;

                var userEmail = superSection["Email"];
                var userPassword = superSection["Password"];

                var superUser = await userManager.CheckAddNewUserAsync(userEmail, userPassword);
            }
        }

        /// <summary>
        /// This will add a user with the given email if they don't all ready exist
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static async Task<IdentityUser> CheckAddNewUserAsync(this UserManager<IdentityUser> userManager, string email, string password)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
                return user;
            user = new IdentityUser { UserName = email, Email = email };
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