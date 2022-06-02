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
        /// <param name="addNewUserIfNotPresent">If true and the logging in user isn't in the AuthP users, then a new AuthP user is created</param>
        /// <param name="authenticationSchemeName">Optional:
        /// Needs to be that same as used in AddAuthentication call - defaults to <see cref="OpenIdConnectDefaults.AuthenticationScheme"/></param>
        /// <returns>AzureAdSettings</returns>
        public static AzureAdSettings AzureAdDefaultSettings(bool addNewUserIfNotPresent, 
            string authenticationSchemeName = OpenIdConnectDefaults.AuthenticationScheme)
        {
            return new AzureAdSettings(
                "http://schemas.microsoft.com/identity/claims/objectidentifier",
                "preferred_username",
                "name",
                addNewUserIfNotPresent, authenticationSchemeName);
        }

        /// <summary>
        /// This lets you define the claim names in an OpenIDConnect to an Azure AD
        /// </summary>
        /// <param name="userIdClaimName"></param>
        /// <param name="emailClaimName"></param>
        /// <param name="usernameClaimName"></param>
        /// <param name="addNewUserIfNotPresent"></param>
        /// <param name="authenticationSchemeName"></param>
        public AzureAdSettings(string userIdClaimName, string emailClaimName, string usernameClaimName,
            bool addNewUserIfNotPresent,
            string authenticationSchemeName)
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
        /// NOTE: This needs a NonRegisterAddUserManager or equivalent service and an event code to set up a user
        /// </summary>
        public bool AddNewUserIfNotPresent { get; set; }

    }
}
