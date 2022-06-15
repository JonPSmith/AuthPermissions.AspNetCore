// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Cryptography;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.SupportCode.AzureAdServices;
using Microsoft.Extensions.Options;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

/// <summary>
/// This implements a user manager when using Azure AD as your authentication handler
/// </summary>
public class AzureAdNewUserManager : IAddNewUserManager
{
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly IAuthTenantAdminService _tenantAdminService;
    private readonly IAzureAdAccessService _azureAccessService;
    private readonly AzureAdOptions _azureOptions;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="authUsersAdmin"></param>
    /// <param name="tenantAdminService"></param>
    /// <param name="azureAccessService"></param>
    /// <param name="azureOptions"></param>
    public AzureAdNewUserManager(IAuthUsersAdminService authUsersAdmin, IAuthTenantAdminService tenantAdminService, 
        IAzureAdAccessService azureAccessService, IOptions<AzureAdOptions> azureOptions)
    {
        _authUsersAdmin = authUsersAdmin;
        _tenantAdminService = tenantAdminService;
        _azureAccessService = azureAccessService;
        _azureOptions = azureOptions.Value;
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
    public AddNewUserDto UserLoginData { get; private set; }

    /// <summary>
    /// This makes a quick check that the user isn't already has an AuthUser 
    /// </summary>
    /// <param name="newUser"></param>
    /// <returns>status, with error if there an user already</returns>
    public async Task<IStatusGeneric> CheckNoExistingAuthUserAsync(AddNewUserDto newUser)
    {
        var status = new StatusGenericHandler();
        if ((await _authUsersAdmin.FindAuthUserByEmailAsync(newUser.Email))?.Result != null)
            return status.AddError("There is already an AuthUser with your email, so you can't add another.",
                nameof(AddNewUserDto.Email));
        return status;
    }

    /// <summary>
    /// This uses the <see cref="IAzureAdAccessService"/> to create the user with an temporary password in the AzureAD.
    /// This returns the userId of the AzureAD user, which is uses to create the <see cref="AuthUser"/> with the Roles, Tenants, etc. 
    /// </summary>
    /// <param name="newUser">The information for creating an AuthUser </param>
    public async Task<IStatusGeneric> SetUserInfoAsync(AddNewUserDto newUser)
    {
        UserLoginData = newUser ?? throw new ArgumentNullException(nameof(newUser));

        var azureUserStatus = await FindOrCreateAzureAdUser(UserLoginData.Email);

        if (azureUserStatus.HasErrors)
            return azureUserStatus;

        //We have found or created the Azure AD user, so we have the user's UserId.
        //Now we create the AuthUser using the data we have been given

        var tenantName = UserLoginData.TenantId == null
            ? null
            : (await _tenantAdminService.GetTenantViaIdAsync((int)UserLoginData.TenantId)).Result?.TenantFullName;

        return await _authUsersAdmin.AddNewUserAsync(azureUserStatus.Result,
            UserLoginData.Email, UserLoginData.UserName, UserLoginData.Roles, tenantName);
    }



    /// <summary>
    /// not used 
    /// </summary>
    /// <returns>status with the final <see cref="AddNewUserDto"/> setting.
    /// This is needed in the Azure AD version, as it creates a temporary password.</returns>
    public Task<IStatusGeneric<AddNewUserDto>> LoginAsync()
    {
        if (UserLoginData == null)
            throw new AuthPermissionsException($"Must call {nameof(SetUserInfoAsync)} before calling this method.");

        var findCreate = UserLoginData.Password == null ? "found" : "registered";
        var status = new StatusGenericHandler<AddNewUserDto>
            { Message = $"Successfully {findCreate} your Azure Ad user. Now login via the 'sign in' link." };
        return Task.FromResult<IStatusGeneric<AddNewUserDto>>(status.SetResult(UserLoginData));
    }

    //--------------------------------------------------
    // private methods

    private async Task<IStatusGeneric<string>> FindOrCreateAzureAdUser(string email)
    {
        var status = new StatusGenericHandler<string>();

        var approaches = _azureOptions.AzureAdApproaches?.Split(',')
                             .Select(x => x.Trim().ToLower()).ToArray()
                         ?? throw new AuthPermissionsException(
                             $"The Azure options in the appsettings file should contain a {nameof(AzureAdOptions.AzureAdApproaches)} part");

        if (approaches.Contains("find"))
        {
            var foundUser =
                await _azureAccessService.FindAzureUserAsync(email);
            if (foundUser != null)
            {
                UserLoginData.Password = null;  //this tells the code to not show a temporary password (used in Create)
                return status.SetResult(foundUser);
            }
        }

        if (approaches.Contains("create"))
        {
            //Create a temporary password for the AzureAD user that is going to be 
            var randomNumber = new byte[20];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            UserLoginData.Password = Convert.ToBase64String(randomNumber);

            var newUserStatus =
                await _azureAccessService.CreateNewUserAsync(UserLoginData.Email, UserLoginData.UserName,
                    UserLoginData.Password);
            return newUserStatus;
        }

        return status.AddError($"Could not {string.Join(" or ", approaches)} the Azure AD user.");

    }
}