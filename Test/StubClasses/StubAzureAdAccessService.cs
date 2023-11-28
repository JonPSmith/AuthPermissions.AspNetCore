// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.SupportCode.AzureAdServices;
using StatusGeneric;

namespace Test.StubClasses;

public class StubAzureAdAccessService : IAzureAdAccessService
{
    /// <summary>
    /// This returns a list of all the enabled users for syncing with the AuthP users
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<SyncAuthenticationUser>> ISyncAuthenticationUsers.GetAllActiveUserInfoAsync()
    {
        return Task.FromResult<IEnumerable<SyncAuthenticationUser>>(new[]
        {
            new SyncAuthenticationUser("User1", "User1@gmail.com", null),
            new SyncAuthenticationUser("User2", "User2@gmail.com", null),
        });
    }

    /// <summary>
    /// This will look for a user with the given email
    /// </summary>
    /// <param name="email"></param>
    /// <returns>if found it returns the user's ID, otherwise it returns null</returns>
    public Task<string> FindAzureUserAsync(string email)
    {
        return Task.FromResult<string>(null);
    }

    /// <summary>
    /// This creates a new user in the Azure AD. It returns the ID of the new Azure AD user.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="userName"></param>
    /// <param name="temporaryPassword"></param>
    /// <returns>status: if error then return message, otherwise Result holds ID of the newly created Azure AD user</returns>
    public Task<IStatusGeneric<string>> CreateNewUserAsync(string email, string userName, string temporaryPassword)
    {
        return Task.FromResult<IStatusGeneric<string>>(new StatusGenericHandler<string>().SetResult("Azure-AD-userId"));
    }
}