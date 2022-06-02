// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using StatusGeneric;

namespace Test.TestHelpers;

public class StubAuthenticationAddUserManager : IAuthenticationAddUserManager
{
    private readonly IAuthUsersAdminService _authUsersAdmin;
    private readonly AuthTenantAdminService _authTenantAdmin;

    public StubAuthenticationAddUserManager(IAuthUsersAdminService usersAdmin, AuthTenantAdminService authTenantAdmin = null)
    {
        _authUsersAdmin = usersAdmin;
        _authTenantAdmin = authTenantAdmin;
    }

    public string AuthenticationGroup { get; } = "Stub";
    public AddNewUserDto UserLoginData { get; }

    public Task<IStatusGeneric> CheckNoExistingAuthUserAsync(AddNewUserDto newUser)
    {
        return Task.FromResult<IStatusGeneric>(new StatusGenericHandler());
    }

    public async Task<IStatusGeneric> SetUserInfoAsync(AddNewUserDto newUser)
    {
        var tenantName = _authTenantAdmin != null && newUser.TenantId != null
            ? (await _authTenantAdmin.GetTenantViaIdAsync((int)newUser.TenantId)).Result.TenantFullName
            : null;

        return await _authUsersAdmin.AddNewUserAsync(newUser.Email,
            newUser.Email, newUser.UserName, newUser.Roles, tenantName);
    }

    public Task<IStatusGeneric<AddNewUserDto>> LoginAsync() => 
        Task.FromResult<IStatusGeneric<AddNewUserDto>>(new StatusGenericHandler<AddNewUserDto>());
}