// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace AuthPermissions.AspNetCore.OpenIdCode
{
    /// <summary>
    /// This contains the names of the claims to get the userId, Email and Username when using Azure AD
    /// </summary>
    public class AzureAdEventSettings
    {
        /// <summary>
        /// This lets you define the claim names in an OpenIDConnect to an Azure AD
        /// </summary>
        /// <param name="userIdClaimName"></param>
        /// <param name="emailClaimName"></param>
        /// <param name="usernameClaimName"></param>
        /// <param name="authenticationSchemeName"></param>
        public AzureAdEventSettings(string userIdClaimName, string emailClaimName, string usernameClaimName,
            string authenticationSchemeName)
        {
            UserIdClaimName = userIdClaimName;
            EmailClaimName = emailClaimName;
            UsernameClaimName = usernameClaimName;
            AuthenticationSchemeName = authenticationSchemeName;
        }

        /// <summary>
        /// Contains the claim name holding the UserId
        /// </summary>
        public string UserIdClaimName { get; }

        /// <summary>
        /// Contains the claim name holding the user's email
        /// </summary>
        public string EmailClaimName { get; }

        /// <summary>
        /// Contains the claim name holding the user's name
        /// </summary>
        public string UsernameClaimName { get; }

        /// <summary>
        /// This holds the AuthenticationScheme Name
        /// </summary>
        public string AuthenticationSchemeName { get; }

        /// <summary>
        /// This provides a standard set of claim names when working with Azure AD
        /// </summary>
        /// <param name="authenticationSchemeName">Optional:
        /// Needs to be that same as used in AddAuthentication call - defaults to <see cref="OpenIdConnectDefaults.AuthenticationScheme"/></param>
        /// <returns>AzureAdEventSettings</returns>
        public static AzureAdEventSettings AzureAdDefaultSettings(
            string authenticationSchemeName = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return new AzureAdEventSettings(
                "http://schemas.microsoft.com/identity/claims/objectidentifier",
                "preferred_username",
                "name",
                authenticationSchemeName);
        }
    }
}
