// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthPermissions.AspNetCore.OpenIdCode
{
    /// <summary>
    /// Extension methods for Azure Ad authentication
    /// </summary>
    public static class AzureAdOpenIdExtension
    {
        /// <summary>
        /// This will register an OpenId Connect event which will add the AuthP's claims to the principal user
        /// </summary>
        /// <param name="services">The service collection to register this to</param>
        /// <param name="settings">This contains the data needed to add the AuthP claims to the Azure AD login</param>
        public static void SetupOpenAzureAdOpenId(this IServiceCollection services, AzureAdSettings settings)
        {
            services.Configure<OpenIdConnectOptions>(settings.AuthenticationSchemeName, options =>
            {
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async ctx => 
                    {
                        string userId = ctx.Principal.FindFirstValue(settings.UserIdClaimName);

                        var authPUserService =
                            ctx.HttpContext.RequestServices.GetRequiredService<IAuthUsersAdminService>();

                        var findStatus = await authPUserService.FindAuthUserByUserIdAsync(userId);
                        if (findStatus.Result == null)
                        {
                            //no user of that name found
                            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<AzureAdSettings>>();

                            if (settings.AddNewUserIfNotPresent)
                            {
                                var email = ctx.Principal.FindFirstValue(settings.EmailClaimName);
                                var username = ctx.Principal.FindFirstValue(settings.UsernameClaimName);

                                var createStatus =
                                    await authPUserService.AddNewUserAsync(userId, email, username, new List<string>());
                                createStatus.IfErrorsTurnToException();

                                logger.LogInformation($"Added a new user with UserId = {userId} on login.");
                            }
                            else
                            {
                                logger.LogWarning($"A user with UserId = {userId} logged in, but was not in the AuthP user database.");
                            }

                            return;
                        }

                        //We have an existing AuthP user, so we add their claims
                        var claimsCalculator =
                            ctx.HttpContext.RequestServices.GetRequiredService<IClaimsCalculator>();

                        var claimsToAdd = await claimsCalculator.GetClaimsForAuthUserAsync(userId);
                        var appIdentity = new ClaimsIdentity(claimsToAdd);
                        ctx.Principal.AddIdentity(appIdentity);

                        return;
                    }
                };
            });
        }
    }
}