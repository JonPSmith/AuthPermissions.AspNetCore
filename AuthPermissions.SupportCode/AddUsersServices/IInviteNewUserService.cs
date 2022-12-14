// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This interface defines the methods for inviting a new user to an application
/// and the code to accept that invite and register the user with AuthP
/// </summary>
public interface IInviteNewUserService
{
    /// <summary>
    /// This provides a selection of expiration times for a user invite.
    /// If you don't like the expiration times you can create your own version of this code
    /// </summary>
    /// <returns></returns>
    List<KeyValuePair<long, string>> ListOfExpirationTimes();

    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// </summary>
    /// <param name="invitedUser">Data needed to add a new AuthP user</param>
    /// <param name="userId">userId of current user - used to obtain any tenant info.</param>
    /// <returns>status with message and encrypted string containing the data to send the user in a link</returns>
    Task<IStatusGeneric<string>> CreateInviteUserToJoinAsync(AddNewUserDto invitedUser, string userId);

    /// <summary>
    /// This takes the information from the user using the invite plus the encrypted invite code.
    /// After a check on the user email is the same as the email in the invite, it then creates
    /// an authentication login / user, which provides the UserId, and then created an AuthUser 
    /// containing the email/username, Roles and Tenant info held in encrypted invite data.
    /// </summary>
    /// <param name="inviteParam">The encrypted part of the url encoded to work with urls
    ///     that was created by <see cref="CreateInviteUserToJoinAsync"/></param>
    /// <param name="email">email - used to check that the user is the same as the invite</param>
    /// <param name="userName">username - used for creating the user</param>
    /// <param name="password">If use are using a register / login authentication handler (e.g. individual user accounts),
    ///     then the password for the new user should be provided</param>
    /// <param name="isPersistent">If use are using a register / login authentication handler (e.g. individual user accounts)
    ///     and you are using authentication cookie, then setting this to true makes the login persistent</param>
    /// <returns>Status with the data used to create the user</returns>
    public Task<IStatusGeneric<AddNewUserDto>> AddUserViaInvite(string inviteParam,
        string email, string userName, string password = null, bool isPersistent = false);
}