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
    private readonly IEncryptDecryptService _encryptorService;
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly IAuthTenantAdminService _tenantAdminService;
    private readonly NonRegisterAddUserManager _addUserLoginService;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="encryptorService"></param>
    /// <param name="authUsersAdmin"></param>
    /// <param name="tenantAdminService"></param>
    /// <param name="addUserLoginService"></param>
    public InviteNewUser(IEncryptDecryptService encryptorService, IAuthUsersAdminService authUsersAdmin, 
        IAuthTenantAdminService tenantAdminService, NonRegisterAddUserManager addUserLoginService,AuthPermissionsOptions options)
    {
        _encryptorService = encryptorService;
        _authUsersAdmin = authUsersAdmin;
        _tenantAdminService = tenantAdminService;
        _addUserLoginService = addUserLoginService;
    }


    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// </summary>
    /// <param name="joiningUser">Data needed to add a new AuthP user</param>
    /// <returns>encrypted string containing the <see cref="InviteNewUser"/> data to send the user in a link</returns>
    public string InviteUserToJoinTenantAsync(InviteNewUser joiningUser)
    {
        var jsonString = JsonSerializer.Serialize(joiningUser);
        var verify = _encryptorService.Encrypt(jsonString);
        return Base64UrlEncoder.Encode(verify);
    }

    /// <summary>
    /// This will take the new user's information plus the encrypted invite code and
    /// 1. decides if the invite matches the user's email
    /// 2. It will create an individual accounts user (if not there), plus a check teh user isn't already an authP user
    /// 3. Then it will create an authP user linked to the tenant they were invited to
    /// NOTE: On return you MUST sign in the user using the email and password they provided via the individual accounts signInManager
    /// </summary>
    /// <param name="email">email given to log in</param>
    /// <param name="password">password given to log in</param>
    /// <param name="inviteParam">The encrypted part of the url encoded to work with urls
    /// that was created by <see cref="InviteUserToJoinTenantAsync"/></param>
    /// <returns>Status with the individual accounts user</returns>
    public async Task<IStatusGeneric<IdentityUser>> AcceptUserJoiningATenantAsync(string email, string password, string inviteParam)
    {
        var status = new StatusGenericHandler<IdentityUser>();

        int tenantId;
        string emailOfJoiner;
        try
        {
            var decrypted = _encryptorService.Decrypt(Base64UrlEncoder.Decode(inviteParam));

            var parts = decrypted.Split(',');
            tenantId = int.Parse(parts[0]);
            emailOfJoiner = parts[1].Trim();
        }
        catch (Exception)
        {
            //Could add a log here
            return status.AddError("Sorry, the verification failed.");
        }

        if (emailOfJoiner != email.Trim())
            return status.AddError("Sorry, your email didn't match the invite.");

        var tenant = await _tenantAdminService.QueryTenants()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId);
        if (tenant == null)
            return status.AddError("Sorry, your invite is rejected. Please talk to your admin person.");

        //Add a new individual users account user, or return existing user
        //Will sent back error if already an AuthUser, because a user can't be linked to multiple tenants
        var userStatus = await GetIndividualAccountUserAndCheckNotAuthUser(email, password);
        if (status.CombineStatuses(userStatus).HasErrors)
            return status;

        //We add the "Tenant User" role to the invited user so that they can access the features
        status.CombineStatuses(await _authUsersAdmin.AddNewUserAsync(userStatus.Result.Id, email, null,
            new List<string> { "Tenant User" }, tenant.TenantFullName));

        if (status.HasErrors)
            return status;

        status.SetResult(userStatus.Result);
        status.Message = $"You have successfully joined the tenant '{tenant.TenantFullName}'";
        return status;
    }
}