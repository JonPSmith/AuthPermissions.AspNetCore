using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace AuthPermissions.AspNetCore.OpenIdCode
{
    /// <summary>
    /// This contains the names of the claims to get the userId, Email and Username when using Azure AD
    /// </summary>
    public class AzureAdSettings
    {
        /// <summary>
        /// This provides a standard set of claim names when working with Azure AD
        /// </summary>
        /// <param name="addNewUserIfNotPresent">Optional: defaults to NOT adding a new user if that user isn't in the AuthP list</param>
        /// <param name="authenticationSchemeName">Optional:
        /// Needs to be that same as used in AddAuthentication call - defaults to <see cref="OpenIdConnectDefaults.AuthenticationScheme"/></param>
        /// <returns></returns>
        public static AzureAdSettings AzureAdDefaultSettings(bool addNewUserIfNotPresent = false, 
            string authenticationSchemeName = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return new AzureAdSettings(
                "http://schemas.microsoft.com/identity/claims/objectidentifier",
                "name",
                "preferred_username",
                authenticationSchemeName,
                addNewUserIfNotPresent);
        }

        /// <summary>
        /// This lets you define the claim names in an OpenIDConnect to an Azure AD
        /// </summary>
        /// <param name="userIdClaimName"></param>
        /// <param name="emailClaimName"></param>
        /// <param name="usernameClaimName"></param>
        /// <param name="authenticationSchemeName"></param>
        /// <param name="addNewUserIfNotPresent"></param>
        public AzureAdSettings(string userIdClaimName, string emailClaimName, string usernameClaimName,
            string authenticationSchemeName, bool addNewUserIfNotPresent)
        {
            UserIdClaimName = userIdClaimName;
            EmailClaimName = emailClaimName;
            UsernameClaimName = usernameClaimName;
            AddNewUserIfNotPresent = addNewUserIfNotPresent;
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
        /// If true and the user isn't known to AuthP, then it will add an new AuthP user using the given data
        /// </summary>
        public bool AddNewUserIfNotPresent { get; }

    }
}
