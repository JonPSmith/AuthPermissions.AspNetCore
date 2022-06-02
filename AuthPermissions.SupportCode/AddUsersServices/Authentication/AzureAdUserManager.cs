// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.SupportCode.AzureAdServices;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

/// <summary>
/// This implements a user manager when using Azure AD as your authentication handler
/// </summary>
public class AzureAdUserManager : IAuthenticationAddUserManager
{
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly IAuthTenantAdminService _tenantAdminService;
    private readonly IAzureAdAccessService _azureAccessService;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="authUsersAdmin"></param>
    /// <param name="tenantAdminService"></param>
    /// <param name="azureAccessService"></param>
    public AzureAdUserManager(IAuthUsersAdminService authUsersAdmin, IAuthTenantAdminService tenantAdminService, IAzureAdAccessService azureAccessService)
    {
        _authUsersAdmin = authUsersAdmin;
        _tenantAdminService = tenantAdminService;
        _azureAccessService = azureAccessService;
    }

    /// <summary>
    /// This tells you what Authentication handler, or group of handlers, that the Add User Manager supports
    /// </summary>
    public string AuthenticationGroup { get; } = "AzureAd";

    /// <summary>
    /// This holds the data provided for the login.
    /// Used to check that the email of the person who will login is the same as the email provided by the user
    /// NOTE: Email and UserName can be null if providing a default value
    /// </summary>
    public AddNewUserDto NewUserLogin { get; private set; }

    /// <summary>
    /// This makes a quick check that the user isn't already has an AuthUser 
    /// </summary>
    /// <param name="newUser"></param>
    /// <returns>status, with error if there an user already</returns>
    public async Task<IStatusGeneric> CheckNoExistingAuthUserAsync(AddNewUserDto newUser)
    {
        var status = new StatusGenericHandler();
        if ((await _authUsersAdmin.FindAuthUserByEmailAsync(newUser.Email))?.Result != null)
            return status.AddError("There is already an AuthUser with your email, so you can't add another.");
        return status;
    }

    /// <summary>
    /// This simply holds this user data, including the AuthP data.
    /// </summary>
    /// <param name="newUser">The information for creating an AuthUser </param>
    public async Task<IStatusGeneric> SetUserInfoAsync(AddNewUserDto newUser)
    {
        NewUserLogin = newUser ?? throw new ArgumentNullException(nameof(newUser));

        var newAzureUserId =
            await _azureAccessService.CreateNewUserAsync(NewUserLogin.Email, NewUserLogin.UserName, NewUserLogin.Password);

        //We have created the Azure AD user, so we have the user's UserId.
        //Now we create the AuthUser using the data we have been given

        var tenantName = NewUserLogin.TenantId == null
            ? null
            : (await _tenantAdminService.GetTenantViaIdAsync((int)NewUserLogin.TenantId)).Result?.TenantFullName;

        return await _authUsersAdmin.AddNewUserAsync(newAzureUserId,
            NewUserLogin.Email, NewUserLogin.UserName, NewUserLogin.Roles, tenantName);
    }

    /// <summary>
    /// not used 
    /// </summary>
    /// <returns>status</returns>
    public Task<IStatusGeneric> LoginAsync()
    {
        var status = new StatusGenericHandler
            { Message = "Successfully registered your user. Now login via the Login link" };
        return Task.FromResult<IStatusGeneric>(status);
    }
}