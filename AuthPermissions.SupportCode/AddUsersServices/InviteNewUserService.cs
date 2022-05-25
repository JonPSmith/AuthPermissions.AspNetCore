// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This service implements the "invite user to join the application" feature
/// </summary>
public class InviteNewUserService : IInviteNewUserService
{
    private readonly IEncryptDecryptService _encryptService;
    private readonly IAuthUsersAdminService _usersAdmin;
    private readonly IAuthenticationAddUserManager _addUserManager;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="encryptService"></param>
    /// <param name="usersAdmin"></param>
    /// <param name="addUserManager"></param>
    public InviteNewUserService(IEncryptDecryptService encryptService, IAuthUsersAdminService usersAdmin,
        IAuthenticationAddUserManager addUserManager)
    {
        _encryptService = encryptService;
        _addUserManager = addUserManager;
        _usersAdmin = usersAdmin;
    }

    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// </summary>
    /// <param name="joiningUser">Data needed to add a new AuthP user</param>
    /// <param name="currentUser">Get the current user to find what tenant (if multi-tenant) the caller is in.</param>
    /// <returns>status with message and encrypted string containing the data to send the user in a link</returns>
    public async Task<IStatusGeneric<string>> InviteUserToJoinTenantAsync(AddUserDataDto joiningUser, ClaimsPrincipal currentUser)
    {
        var status = new StatusGenericHandler<string>();

        if (currentUser == null)
            throw new ArgumentNullException(nameof(currentUser));

        var authUserStatus = await _usersAdmin.FindAuthUserByUserIdAsync(currentUser.GetUserIdFromUser());
        if (authUserStatus.Result == null)
            throw new AuthPermissionsException("User must be registered with AuthP");

        joiningUser.TenantId = authUserStatus.Result.TenantId;
        status.Message =
            $"Please send the url to the user '{joiningUser.Email ?? joiningUser.UserName}' which allow them to join" +
            (joiningUser.TenantId == null
                ? "your application"
                : $"the tenant {authUserStatus.Result.UserTenant.TenantFullName}.");

        var jsonString = JsonSerializer.Serialize(joiningUser);
        var verify = _encryptService.Encrypt(jsonString);

        status.SetResult(Base64UrlEncoder.Encode(verify));
        return status;
    }

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
    public async Task<IStatusGeneric> AddUserViaInvite(string inviteParam, 
        string email, string password = null, bool isPersistent = false)
    {
        var status = new StatusGenericHandler<IdentityUser>();
        var normalizedEmail = email.Trim().ToLower();

        AddUserDataDto inviteData;
        try
        {
            var decrypted = _encryptService.Decrypt(Base64UrlEncoder.Decode(inviteParam));
            inviteData = JsonSerializer.Deserialize<AddUserDataDto>(decrypted);
        }
        catch (Exception)
        {
            //Could add a log here
            return status.AddError("Sorry, the verification failed.");
        }

        if (inviteData.Email!= normalizedEmail)
            return status.AddError("Sorry, your email didn't match the invite.");

        status.CombineStatuses(await _addUserManager.SetUserInfoAsync(inviteData, password));

        if (status.HasErrors)
            return status;

        return await _addUserManager.LoginVerificationAsync(inviteData.Email, inviteData.UserName, isPersistent);
    }
}