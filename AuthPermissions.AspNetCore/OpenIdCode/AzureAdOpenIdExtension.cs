// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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
                        var email = ctx.Principal.FindFirstValue(settings.EmailClaimName);

                        var authPUserService =
                            ctx.HttpContext.RequestServices.GetRequiredService<IAuthUsersAdminService>();

                        var findStatus = await authPUserService.FindAuthUserByUserIdAsync(userId);
                        if (findStatus.Result == null)
                        {
                            //no user of that name found
                            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<AzureAdSettings>>();

                            if (settings.AddNewUserIfNotPresent)
                            {
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

                            //We replace some of the claims in the ClaimPrincipal so that the claims match what AuthP expects
                            CreateClaimPrincipalWithAuthPClaims(ctx, userId, email);
                        }
                        else
                        {
                            //We have an existing AuthP user, so we add their claims
                            var claimsCalculator =
                                ctx.HttpContext.RequestServices.GetRequiredService<IClaimsCalculator>();

                            CreateClaimPrincipalWithAuthPClaims(ctx, userId, email, await claimsCalculator.GetClaimsForAuthUserAsync(userId));
                        }
                    }
                };
            });
        }

        private static void CreateClaimPrincipalWithAuthPClaims(TokenValidatedContext ctx,
            string userId, string email, List<Claim> claimsToAdd = null)
        {
            var updatedClaims = ctx.Principal.Claims.ToList();
            
            if(claimsToAdd != null)
                //add the AuthP claims
                updatedClaims.AddRange(claimsToAdd);

            //NOTE: The ClaimTypes.NameIdentifier is expected to contain the UserId, but with AzureId you get another value
            //Therefore we remove/replace the NameIdentifier claim to have the user's id
            updatedClaims.Remove(
                updatedClaims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier));
            updatedClaims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            //NOTE: We need to provide the Name claim to get the correct name shown in the ASP.NET Core sign in/sign out display
            updatedClaims.Add(new Claim(ClaimTypes.Name, email));

            //now we create a new ClaimsIdentity to replace the existing Principal
            var appIdentity = new ClaimsIdentity(updatedClaims, ctx.Principal.Identity.AuthenticationType);
            ctx.Principal = new ClaimsPrincipal(appIdentity);
        }
    }
}