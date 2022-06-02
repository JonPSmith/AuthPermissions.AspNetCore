// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;

namespace AuthPermissions.SupportCode.AzureAdServices;

/// <summary>
/// Defines the methods in the Azure AD service
/// </summary>
public interface IAzureAdAccessService
{
    /// <summary>
    /// This returns a list of all the enabled users for syncing with the AuthP users
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<SyncAuthenticationUser>> GetAllActiveUserInfoAsync();

    /// <summary>
    /// This creates a new user in the Azure AD. It returns the ID of the new Azure AD user.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns>returns the ID of the newly created Azure AD user</returns>
    Task<string> CreateNewUserAsync(string email, string userName, string password );
}