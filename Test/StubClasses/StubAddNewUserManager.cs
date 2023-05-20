// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using StatusGeneric;

namespace Test.StubClasses;

public class StubAddNewUserManager : IAddNewUserManager
{
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly AuthTenantAdminService _authTenantAdmin;
    private readonly bool _loginReturnsError;

    public StubAddNewUserManager(IAuthUsersAdminService usersAdmin, 
        AuthTenantAdminService authTenantAdmin = null,
        bool loginReturnsError = false)
    {
        _authUsersAdmin = usersAdmin;
        _authTenantAdmin = authTenantAdmin;
        _loginReturnsError = loginReturnsError;
    }

    public string AuthenticationGroup { get; } = "Stub";
    public AddNewUserDto UserLoginData { get; }

    public Task<IStatusGeneric> CheckNoExistingAuthUserAsync(AddNewUserDto newUser)
    {
        return Task.FromResult<IStatusGeneric>(new StatusGenericHandler());
    }

    public async Task<IStatusGeneric<AuthUser>> SetUserInfoAsync(AddNewUserDto newUser)
    {
        var tenantName = _authTenantAdmin != null && newUser.TenantId != null
            ? (await _authTenantAdmin.GetTenantViaIdAsync((int)newUser.TenantId)).Result.TenantFullName
            : null;

        return await _authUsersAdmin.AddNewUserAsync(newUser.Email,
            newUser.Email, newUser.UserName, newUser.Roles, tenantName);
    }

    public Task<IStatusGeneric<AddNewUserDto>> LoginAsync()
    {
        var status = new StatusGenericHandler<AddNewUserDto>();
        if (_loginReturnsError)
            status.AddError("Error in Login");

        return Task.FromResult<IStatusGeneric<AddNewUserDto>>(status);
    }

    /// <summary>
    /// If something happens that makes the user invalid, then this will remove the AuthUser.
    /// Used in <see cref="SignInAndCreateTenant"/> if something goes wrong and we want to undo the tenant
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<IStatusGeneric> RemoveAuthUserAsync(string userId)
    {
        return _authUsersAdmin.DeleteUserAsync(userId);
    }
}