// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This service implements the "invite user to join the application" feature
/// </summary>
public class InviteNewUser
{
    private readonly IEncryptDecryptService _encryptService;
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly IAuthTenantAdminService _tenantAdminService;
    private readonly IAuthenticationAddUserManager _addUserManager;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="encryptService"></param>
    /// <param name="authUsersAdmin"></param>
    /// <param name="tenantAdminService"></param>
    /// <param name="addUserLoginService"></param>
    /// <param name="addUserManager"></param>
    public InviteNewUser(IEncryptDecryptService encryptService, IAuthUsersAdminService authUsersAdmin, 
        IAuthTenantAdminService tenantAdminService, NonRegisterAddUserManager addUserLoginService,
        IAuthenticationAddUserManager addUserManager)
    {
        _encryptService = encryptService;
        _authUsersAdmin = authUsersAdmin;
        _tenantAdminService = tenantAdminService;
        _addUserManager = addUserManager;
    }

    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// </summary>
    /// <param name="joiningUser">Data needed to add a new AuthP user</param>
    /// <returns>encrypted string containing the <see cref="InviteNewUser"/> data to send the user in a link</returns>
    public string InviteUserToJoinTenantAsync(AddUserData joiningUser)
    {
        var jsonString = JsonSerializer.Serialize(joiningUser);
        var verify = _encryptService.Encrypt(jsonString);
        return Base64UrlEncoder.Encode(verify);
    }

    /// <summary>
    /// This will take the new user's information plus the encrypted invite code and
    /// allows the user to create an authentication login, and that created user has
    /// an AuthUser containing the email/username, Roles and Tenant info held in joining data
    /// </summary>
    /// <param name="email">email given to log in</param>
    /// <param name="password">password given to log in</param>
    /// <param name="inviteParam">The encrypted part of the url encoded to work with urls
    ///     that was created by <see cref="InviteUserToJoinTenantAsync"/></param>
    /// <param name="isPersistent">true if cookie should be persistent</param>
    /// <returns>Status with the individual accounts user</returns>
    public async Task<IStatusGeneric> AddUserViaInvite(string email, string password, 
        string inviteParam, bool isPersistent = false)
    {
        var status = new StatusGenericHandler<IdentityUser>();

        var normalizedEmail = email.Trim().ToLower();

        AddUserData inviteData;
        try
        {
            var decrypted = _encryptService.Decrypt(Base64UrlEncoder.Decode(inviteParam));
            inviteData = JsonSerializer.Deserialize<AddUserData>(decrypted);
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