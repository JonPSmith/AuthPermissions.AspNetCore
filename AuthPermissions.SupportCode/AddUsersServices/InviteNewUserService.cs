// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Microsoft.EntityFrameworkCore;
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
    private readonly IAddNewUserManager _addNewUserManager;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="context"></param>
    /// <param name="encryptService"></param>
    /// <param name="usersAdmin"></param>
    /// <param name="addNewUserManager"></param>
    public InviteNewUserService(AuthPermissionsOptions options, AuthPermissionsDbContext context,
        IEncryptDecryptService encryptService,
        IAuthUsersAdminService usersAdmin, IAddNewUserManager addNewUserManager)
    {
        _options = options;
        _context = context;
        _encryptService = encryptService;
        _usersAdmin = usersAdmin;
        _addNewUserManager = addNewUserManager;
    }

    /// <summary>
    /// This creates an encrypted string containing the information containing the
    /// invited user's email (for checking) and the AuthP user settings needed to create am AuthP user
    /// Normally the tenantId is set from the user creating the invite, but there are two exceptions
    /// - Invite user must allow for a Hierarchical tenant where the tenant is deeper than the admin user
    /// - If the user is an non-tenant user (app admin), then the tenantId can be set to null or any tenant
    /// </summary>
    /// <param name="invitedUser">Data needed to add a new AuthP user</param>
    /// <param name="userId">userId of current user - used to obtain any tenant info.</param>
    /// <returns>status with message and encrypted string containing the data to send the user in a link</returns>
    public async Task<IStatusGeneric<string>> CreateInviteUserToJoinAsync(AddNewUserDto invitedUser, string userId)
    {
        var status = new StatusGenericHandler<string>();

        if (userId == null)
            throw new ArgumentNullException(nameof(userId));

        var inviterStatus = await _usersAdmin.FindAuthUserByUserIdAsync(userId);
        if (inviterStatus.Result == null)
            throw new AuthPermissionsException("User must be registered with AuthP");

        if (string.IsNullOrEmpty(invitedUser.Email) && string.IsNullOrEmpty(invitedUser.UserName))
            return status.AddError("You must provide an email or username for the invitation.",
                nameof(AddNewUserDto.Email), nameof(AddNewUserDto.UserName));

        Tenant foundTenant = null;
        if (_options.TenantType.IsMultiTenant())
        {
            //we need to check / set the the tenantId

            if (inviterStatus.Result.TenantId == null)
            {
                //the inviter is an app admin, so they can set any tenant including null
                //but for security reasons they can't invite a app user (it's just too easy to not provide the TenantId)
            }
            else
            {
                //The inviter is a tenant admin 

                if (_options.TenantType.IsSingleLevel())
                {
                    //The tenantId is set from the tenant admin user
                    invitedUser.TenantId = inviterStatus.Result.TenantId;
                }
                else
                {
                    //its hierarchical so we check that the tenantId 
                    foundTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == invitedUser.TenantId);
                    //Check that the tenant is within the scope of the inviting user 
                    if (foundTenant != null && !foundTenant.GetTenantDataKey()
                            .StartsWith(inviterStatus.Result.UserTenant.GetTenantDataKey()))
                        return status.AddError("The Tenant you have selected isn't within your group.",
                            nameof(AddNewUserDto.TenantId));
                }

                if (invitedUser.TenantId == null)
                    return status.AddError("You forgot to select a tenant for the invite.",
                        nameof(AddNewUserDto.TenantId));
            }

            if (invitedUser.TenantId != null) //if app user, then doesn't have a tenant
            {
                //check that the tenantId is valid
                foundTenant ??= await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == invitedUser.TenantId);
                if (foundTenant == null)
                    return status.AddError("The tenant you selected isn't correct.",
                        nameof(AddNewUserDto.TenantId));

                if (invitedUser.Roles != null)
                {
                    //Check that the Roles for the invited user are acceptable for a tenant user
                    var badRoles = await _context.RoleToPermissions.Where(x =>
                            invitedUser.Roles.Contains(x.RoleName) 
                            && (x.RoleType == RoleTypes.HiddenFromTenant || x.RoleType == RoleTypes.TenantAutoAdd))
                        .Select(x => x.RoleName).ToListAsync();
                    if (badRoles.Any())
                        return status.AddError("The following Roles aren't allowed for a tenant user: "+string.Join(", ", badRoles),
                            nameof(AddNewUserDto.Roles));
                }
            }
        }

        if (invitedUser.Roles == null || !invitedUser.Roles.Any())
            return status.AddError(
                "You haven't set up the Roles for the invited user. If you really what that, then select the "
                + $"'{CommonConstants.EmptyItemName}' dropdown item.",
                nameof(AddNewUserDto.Roles));

        status.Message =
            invitedUser.TenantId == null && _options.TenantType.IsMultiTenant()
                ? "WARNING: you are creating an invite that will make the user an app admin (i.e. not a tenant). " +
                  "This is allowable, but wanted to make sure that what you want to do."
                : $"Please send the url to the user '{invitedUser.Email ?? invitedUser.UserName}' which allow them to join " +
                  (invitedUser.TenantId == null
                      ? "your application."
                      : $"the tenant '{foundTenant.TenantFullName}'.");

        //This setting makes the string shorter
        JsonSerializerOptions options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
        var jsonString = JsonSerializer.Serialize(invitedUser, options);
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
    /// <param name="userName">username - used for creating the user</param>
    /// <param name="password">If use are using a register / login authentication handler (e.g. individual user accounts),
    /// then the password for the new user should be provided</param>
    /// <param name="isPersistent">If use are using a register / login authentication handler (e.g. individual user accounts)
    /// and you are using authentication cookie, then setting this to true makes the login persistent</param>
    /// <returns>Status with the data used to create the user</returns>
    public async Task<IStatusGeneric<AddNewUserDto>> AddUserViaInvite(string inviteParam, 
    string email, string userName, string password = null, bool isPersistent = false)
    {
        var status = new StatusGenericHandler<AddNewUserDto>();
        var normalizedEmail = email.Trim().ToLower();

        AddNewUserDto newUserData;
        try
        {
            var decrypted = _encryptService.Decrypt(Base64UrlEncoder.Decode(inviteParam));
            newUserData = JsonSerializer.Deserialize<AddNewUserDto>(decrypted);
        }
        catch (Exception e)
        {
            //Could add a log here
            return status.AddError("Sorry, the verification failed.");
        }

        if (newUserData.Email!= normalizedEmail)
            return status.AddError("Sorry, your email didn't match the invite.",
                nameof(AddNewUserDto.Email));

        newUserData.UserName = userName;
        newUserData.Password = password;
        newUserData.IsPersistent = isPersistent;

        if (status.HasErrors)
            return status;

        status.CombineStatuses(await _addNewUserManager.SetUserInfoAsync(newUserData));

        if (status.HasErrors)
            return status;

        return await _addNewUserManager.LoginAsync(); //This returns the final AddNewUserDto settings
    }
}