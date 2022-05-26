// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using StatusGeneric;

namespace Test.TestHelpers;

public class StubAuthenticationAddUserManager : IAuthenticationAddUserManager
{
    private readonly IAuthUsersAdminService _authUsersAdmin;

    public StubAuthenticationAddUserManager(IAuthUsersAdminService usersAdmin)
    {
        _authUsersAdmin = usersAdmin;
    }

    public string AuthenticationGroup { get; } = "Stub";
    public AddUserDataDto UserLoginData { get; }

    public Task<IStatusGeneric> CheckNoExistingAuthUserAsync(AddUserDataDto userData)
    {
        return Task.FromResult<IStatusGeneric>(new StatusGenericHandler());
    }

    public async Task<IStatusGeneric> SetUserInfoAsync(AddUserDataDto userData, string password = null)
    {
        return await _authUsersAdmin.AddNewUserAsync(userData.Email,
            userData.Email, userData.UserName, userData.Roles);
    }

    public Task<IStatusGeneric> LoginVerificationAsync(string givenEmail, string givenUserName, bool isPersistent = false)
    {
        return Task.FromResult<IStatusGeneric>(new StatusGenericHandler());
    }
}