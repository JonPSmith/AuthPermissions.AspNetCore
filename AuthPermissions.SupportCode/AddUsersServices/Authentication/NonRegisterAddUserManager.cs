// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

/// <summary>
/// This provides a way to allow external authentication handlers where you can't get a user's data
/// before you log in, which means the AuthUser has to created within the login event 
/// This is used by the "invite user" and "sign on" features 
/// </summary>
public class NonRegisterAddUserManager : IAuthenticationAddUserManager
{
    private readonly AuthPermissionsDbContext _authPContext;

    /// <summary>
    /// ctor - used as a service
    /// </summary>
    /// <param name="authPContext"></param>
    public NonRegisterAddUserManager(AuthPermissionsDbContext authPContext)
    {
        _authPContext = authPContext;
    }

    /// <summary>
    /// This catches a user trying to add the same user while another user add is currently running
    /// </summary>
    public int TimeoutSecondsOnSameBeingAdded { get; set; } = 30;

    public AddUserData UserLoginData { get; }

    /// <summary>
    /// This returns true if there is no AuthP user with that email.
    /// This is used to stop an AuthUser being registered again (which would fail) 
    /// </summary>
    /// <param name="email">email of the user. Can be null if userName is provided</param>
    /// <param name="userName">Optional username</param>
    /// <returns></returns>
    public async Task<bool> CheckNoAuthUserAsync(string email, string userName = null)
    {
        return !await _authPContext.AuthUsers
            .AnyAsync(x => (x.Email != null && x.Email == email) || (x.UserName != null && x.UserName == userName));
    }

    /// <summary>
    /// This adds a entry to the database with the user's email and the AuthP Roles / Tenant data for creating the AuthP user
    /// </summary>
    /// <param name="userData">The information for creating an AuthUser </param>
    /// <param name="password">not used with NonRegister authentication handlers</param>
    public async Task<IStatusGeneric> SetUserInfoAsync(AddUserData userData, string password = null)
    {
        var status = new StatusGenericHandler();

        async Task<AddNewUserInfo> AddNewUserInfoToDatabaseAsync()
        {
            var addNewUserInfo = new AddNewUserInfo(userData.Email, userData.UserName,
                userData.GetRolesAsCommaDelimited(), userData.TenantId);
            _authPContext.Add(addNewUserInfo);
            status.CombineStatuses(await _authPContext.SaveChangesWithChecksAsync());
            return addNewUserInfo;
        }

        var newUserInfo = await AddNewUserInfoToDatabaseAsync();
        while (status.HasErrors)
        {
            //most likely an old attempt to log on didn't work, but we need to compare their datetime

            var oldMatching = await _authPContext.AddNewUserInfos
                .SingleOrDefaultAsync(x => (x.Email != null && x.Email == userData.Email) ||
                                           (x.UserName != null && x.UserName == userData.UserName));
            if (oldMatching == null)
                return status; //something bad wrong, so sent back the save status

            if (oldMatching == newUserInfo)
                //the user data is the same, so we can use this
                break;

            if (newUserInfo.CreatedAtUtc.Subtract(oldMatching.CreatedAtUtc).TotalSeconds <
                TimeoutSecondsOnSameBeingAdded)
                return status.AddError("You have an failed try to register / login still in process. " +
                                       $"Wait {TimeoutSecondsOnSameBeingAdded} seconds and try again.");

            //else the matching is old so we delete it 
            _authPContext.Remove(oldMatching);
            await AddNewUserInfoToDatabaseAsync(); //this calls SaveChanges, so the old version will be removed

        } //end of while

        return status;
    }

    /// <summary>
    /// This doesn't do anything with non-register authentication manager
    /// </summary>
    public Task<IStatusGeneric> LoginUserWithVerificationAsync(string givenEmail, string givenUserName, bool isPersistent)
    {
        return Task.FromResult<IStatusGeneric>(new StatusGenericHandler());
    }
}