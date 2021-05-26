// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ExamplesCommonCode.DemoSetupCode
{
    internal static class AspNetUserExtension
    {
        public static async Task<List<IdentityUser>> AddDemoUsersFromJson(this IServiceProvider serviceProvider,
            DemoSetup demoData)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var result = new List<IdentityUser>();
            foreach (var userInfo in demoData.Users)
            {
                var user = await userManager.CheckAddNewUserAsync(userInfo.Email, userInfo.Email);
                result.Add(user);
                if (demoData.AddRolesToAspNetUser && !string.IsNullOrEmpty(userInfo.RolesCommaDelimited))
                {
                    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    foreach (var roleName in userInfo.RolesCommaDelimited.Split(',').Select(x => x.Trim()))
                    {
                        var roleExist = await roleManager.RoleExistsAsync(roleName);
                        if (!roleExist)
                        {
                            //create the roles and seed them to the database: Question 1
                            await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                        await userManager.AddToRoleAsync(user, roleName);
                    }
                }
            }

            return result;
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