// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This service implements the "invite user" feature
/// </summary>
public class InviteNewUserService : IInviteNewUserService
{
    private readonly IEncryptDecryptService _encryptService;
    private readonly AuthPermissionsDbContext _context;
    private readonly IAuthUsersAdminService _usersAdmin;
    private readonly AuthPermissionsOptions _options;
    private readonly IAuthenticationAddUserManager _addUserManager;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="context"></param>
    /// <param name="encryptService"></param>
    /// <param name="usersAdmin"></param>
    /// <param name="addUserManager"></param>
    public InviteNewUserService(AuthPermissionsOptions options, AuthPermissionsDbContext context,
        IEncryptDecryptService encryptService,
        IAuthUsersAdminService usersAdmin, IAuthenticationAddUserManager addUserManager)
    {
        _options = options;
        _context = context;
        _encryptService = encryptService;
        _usersAdmin = usersAdmin;
        _addUserManager = addUserManager;
    }

    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// Normally the tenantId is set from the user creating the invite, but there are two exceptions
    /// - Invite user must allow for a Hierarchical tenant where the tenant is deeper than the admin user
    /// - If the user is an non-tenant user (app admin), then the tenantId can be set to null or any tenant
    /// </summary>
    /// <param name="joiningUser">Data needed to add a new AuthP user</param>
    /// <param name="userId">userId of current user - used to obtain any tenant info.</param>
    /// <returns>status with message and encrypted string containing the data to send the user in a link</returns>
    public async Task<IStatusGeneric<string>> CreateInviteUserToJoinAsync(AddUserDataDto joiningUser, string userId)
    {
        var status = new StatusGenericHandler<string>();

        if (userId == null)
            throw new ArgumentNullException(nameof(userId));

        var inviterStatus = await _usersAdmin.FindAuthUserByUserIdAsync(userId);
        if (inviterStatus.Result == null)
            throw new AuthPermissionsException("User must be registered with AuthP");

        Tenant foundTenant = null;
        if (_options.TenantType.IsMultiTenant())
        {
            //we need to check / set the the tenantId

            if (joiningUser.TenantId != null)
            {
                if (inviterStatus.Result.TenantId == null)
                {
                    // An tenant admin user is sending the invite, so any valid tenantId is allowed
                }
                else if (_options.TenantType.IsSingleLevel())
                {
                    //The tenantId is set from the tenant admin user
                    joiningUser.TenantId = inviterStatus.Result.TenantId;
                }
                else
                {
                    //its hierarchical so we check that the tenantId 
                    foundTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == joiningUser.TenantId);
                    //Check that the tenant is within the scope of the inviting user 
                    if (foundTenant != null && !foundTenant.GetTenantDataKey()
                            .StartsWith(inviterStatus.Result.UserTenant.GetTenantDataKey()))
                        return status.AddError("The Tenant you have selected isn't within your group.");
                }
            }
            else if (inviterStatus.Result.TenantId != null)
            {
                return status.AddError("You must select a tenant for the invite.");
            }
            
            //check that the tenantId (if not null) refers to a actual tenant
            if (joiningUser.TenantId != null)
            {
                foundTenant ??= await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == joiningUser.TenantId);
                if (foundTenant == null)
                    return status.AddError("you forgot to select a tenant for the invite (or the tenant is invalid).");
            }
        }

        status.Message =
            $"Please send the url to the user '{joiningUser.Email ?? joiningUser.UserName}' which allow them to join" +
            (joiningUser.TenantId == null
                ? "your application."
                : $"the tenant '{foundTenant.TenantFullName}'.");

        var jsonString = JsonSerializer.Serialize(joiningUser);
        var verify = _encryptService.Encrypt(jsonString);

        status.SetResult(Base64UrlEncoder.Encode(verify));
        return status;
    }

    /// <summary>
    /// This takes the information from the user using the invite plus the encrypted invite code.
    /// After a check on the user email is the same as the email in the invite, it then creates
    /// an authentication login / user, which provides the UserId, and then created an AuthUser 
    /// containing the email/username, Roles and Tenant info held in encrypted invite data.
    /// </summary>
    /// <param name="inviteParam">The encrypted part of the url encoded to work with urls
    ///     that was created by <see cref="CreateInviteUserToJoinAsync"/></param>
    /// <param name="email">email - used to check that the user is the same as the invite</param>
    /// <param name="password">If use are using a register / login authentication handler (e.g. individual user accounts),
    /// then the password for the new user should be provided</param>
    /// <param name="isPersistent">If use are using a register / login authentication handler (e.g. individual user accounts)
    /// and you are using authentication cookie, then setting this to true makes the login persistent</param>
    /// <returns>Status</returns>
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