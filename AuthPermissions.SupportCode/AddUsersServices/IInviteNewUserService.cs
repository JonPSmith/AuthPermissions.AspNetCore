// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This interface defines the methods for inviting a new user to an application
/// and the code to accept that invite and register the user with AuthP
/// </summary>
public interface IInviteNewUserService
{
    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// </summary>
    /// <param name="joiningUser">Data needed to add a new AuthP user</param>
    /// <param name="currentUser">Get the current user to find what tenant (if multi-tenant) the caller is in.</param>
    /// <returns>status with message and encrypted string containing the data to send the user in a link</returns>
    Task<IStatusGeneric<string>> InviteUserToJoinTenantAsync(AddUserDataDto joiningUser, ClaimsPrincipal currentUser);

    /// <summary>
    /// This will take the new user's information plus the encrypted invite code and
    /// allows the user to create an authentication login, and that created user has
    /// an AuthUser containing the email/username, Roles and Tenant info held in joining data
    /// </summary>
    /// <param name="inviteParam">The encrypted part of the url encoded to work with urls
    ///     that was created by <see cref="InviteUserToJoinTenantAsync"/></param>
    /// <param name="email">email - used to check that the user is the same as the invite</param>
    /// <param name="password">If use are using a register / login authentication handler (e.g. individual user accounts),
    /// then the password for the new user should be provided</param>
    /// <param name="isPersistent">If use are using a register / login authentication handler (e.g. individual user accounts)
    /// and you are using authentication cookie, then setting this to true makes the login persistent</param>
    /// <returns>Status with the individual accounts user</returns>
    Task<IStatusGeneric> AddUserViaInvite(string inviteParam,
        string email, string password = null, bool isPersistent = false);
}