// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AzureAdServices;

/// <summary>
/// Defines the methods in the Azure AD service, including the <see cref="ISyncAuthenticationUsers"/>
/// </summary>
public interface IAzureAdAccessService : ISyncAuthenticationUsers
{
    /// <summary>
    /// This will look for a user with the given email
    /// </summary>
    /// <param name="email"></param>
    /// <returns>if found it returns the user's ID, otherwise it returns null</returns>
    Task<string> FindAzureUserAsync(string email);

    /// <summary>
    /// This creates a new user in the Azure AD. It returns the ID of the new Azure AD user.
    /// </summary>
    /// <param name="email">Must be provided</param>
    /// <param name="userName">Must be provided</param>
    /// <param name="temporaryPassword">Must be present. It is a temporary Password</param>
    /// <returns>status: if error then return message, otherwise Result holds ID of the newly created Azure AD user</returns>
    Task<IStatusGeneric<string>> CreateNewUserAsync(string email, string userName, string temporaryPassword);
}