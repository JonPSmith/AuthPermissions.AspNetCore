// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.AdminCode;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

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
        /// <param name="eventSettings">This contains the data needed to add the AuthP claims to the Azure AD login</param>
        public static void SetupOpenAzureAdOpenId(this IServiceCollection services, AzureAdEventSettings eventSettings)
        {
            services.Configure<OpenIdConnectOptions>(eventSettings.AuthenticationSchemeName, options =>
            {
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        string userId = context.Principal.FindFirstValue(eventSettings.UserIdClaimName);
                        var email = context.Principal.FindFirstValue(eventSettings.EmailClaimName);

                        //The claims as set by AzureAD don't match what ASP.NET Core / AuthP needs so sort it out
                        var updatedClaims = context.Principal.Identities.First().Claims.ToList();

                        //The ClaimTypes.NameIdentifier is expected to contain the UserId, but with AzureId you get another value
                        //Therefore we remove/replace the NameIdentifier claim to have the user's id
                        updatedClaims.Remove(
                            updatedClaims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier));
                        updatedClaims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

                        //We need to provide the Name claim to get the correct name shown in the ASP.NET Core sign in/sign out display
                        //NOTE: Could have used AzureAD "name" instead of email
                        updatedClaims.Add(new Claim(ClaimTypes.Name, email));


                        #region AuthPRegion
                        //----------------------------------------------------------------------------------
                        //This is the section where the AuthP code has to go

                        //This version looks for an AuthUser linked to this user. If found it adds the claims for that user

                        var authPUserService =
                            context.HttpContext.RequestServices.GetRequiredService<IAuthUsersAdminService>();

                        var findStatus = await authPUserService.FindAuthUserByUserIdAsync(userId);
                        if (findStatus.Result != null)
                        {
                            //We have an existing AuthP user, so we add their claims
                            var claimsCalculator =
                                context.HttpContext.RequestServices.GetRequiredService<IClaimsCalculator>();
                            var claimsToAdd = await claimsCalculator.GetClaimsForAuthUserAsync(userId);
                            updatedClaims.AddRange(claimsToAdd);
                        }

                        //-----------------------------------------------------------------------------------
                        #endregion

                        //now we create a new ClaimsIdentity to replace the existing Principal
                        var appIdentity = new ClaimsIdentity(updatedClaims, context.Principal.Identity.AuthenticationType);
                        context.Principal = new ClaimsPrincipal(appIdentity);
                    }
                };
            });
        }
    }
}