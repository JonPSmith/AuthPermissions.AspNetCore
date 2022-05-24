// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly Logger<NonRegisterAddUserManager> _logger;

    /// <summary>
    /// ctor - used as a service
    /// </summary>
    /// <param name="authPContext"></param>
    /// <param name="logger"></param>
    public NonRegisterAddUserManager(AuthPermissionsDbContext authPContext, Logger<NonRegisterAddUserManager> logger)
    {
        _authPContext = authPContext;
        _logger = logger;
    }
    /// <summary>
    /// This Add User Manager supports Authentication handlers where you can't register the user before they log in
    /// e.g., Social logins like Google, Twitter etc. NOTE: These need extra code that is called in a login event
    /// </summary>
    public string AuthenticationGroup { get; } = "HandlersWithoutRegistration";

    /// <summary>
    /// This catches a user trying to add the same user while another user add is currently running
    /// </summary>
    public int TimeoutSecondsOnSameBeingAdded { get; set; } = 30;

    /// <summary>
    /// This holds the data provided for the login.
    /// Used to check that the email of the person who will login is the same as the email provided by the user
    /// NOTE: Email and UserName can be null if providing a default value
    /// </summary>
    public AddUserDataDto UserLoginData { get; private set; }

    /// <summary>
    /// This makes a quick check that the user isn't already has an AuthUser 
    /// </summary>
    /// <param name="userData"></param>
    /// <returns>status, with error if there an user already</returns>
    public async Task<IStatusGeneric> CheckNoExistingAuthUser(AddUserDataDto userData)
    {
        var status = new StatusGenericHandler();
        if (await _authPContext.AuthUsers
                .AnyAsync(x => (x.Email != null && x.Email == userData.Email)
                               || (x.UserName != null && x.UserName == userData.UserName)))
            return status.AddError("There is already an AuthUser with your email / username, so you can't add another."); ;
        return status;
    }

    /// <summary>
    /// This adds a entry to the database with the user's email and the AuthP Roles / Tenant data for creating the AuthP user
    /// </summary>
    /// <param name="userData">The information for creating an AuthUser </param>
    /// <param name="password">not used with NonRegister authentication handlers</param>
    public async Task<IStatusGeneric> SetUserInfoAsync(AddUserDataDto userData, string password = null)
    {
        UserLoginData = userData ?? throw new ArgumentNullException(nameof(userData));

        var status = new StatusGenericHandler();

        async Task<AddNewUserInfo> AddNewUserInfoToDatabaseAsync()
        {
            var addNewUserInfo = new AddNewUserInfo(userData.Email, userData.UserName,
                userData.GetRolesAsCommaDelimited(), userData.TenantId);
            _authPContext.Add(addNewUserInfo);
            status.CombineStatuses(await _authPContext.SaveChangesWithChecksAsync());
            return addNewUserInfo;
        }

        if (await _authPContext.AuthUsers
                .AnyAsync(x => (x.Email != null && x.Email == userData.Email) 
                               || (x.UserName != null && x.UserName == userData.UserName)))
            return status.AddError("There is already an AuthUser with your email / username, so you can't add another.");

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
    /// This can't login, but it check that the expected AuthUser is there
    /// </summary>
    /// <param name="givenEmail">Ignored</param>
    /// <param name="givenUserName">Ignored</param>
    /// <param name="isPersistent">Ignored</param>
    /// <returns>status</returns>
    public async Task<IStatusGeneric> LoginVerificationAsync(string givenEmail, string givenUserName, bool isPersistent)
    {
        if (UserLoginData == null)
            throw new AuthPermissionsException($"Must call {nameof(SetUserInfoAsync)} before calling this method.");

        var status = new StatusGenericHandler { Message = "Checked OK. Will set up claims when you log in." };

        var expectedAuthUser = await _authPContext.AuthUsers
            .SingleOrDefaultAsync(x => x.Email == UserLoginData.Email);

        if (expectedAuthUser != null) 
            //all OK
            return status;

        //Alert the user and the admin people (via a log) that the add of an AuthUser failed
        var authInfoForUser = await _authPContext.AddNewUserInfos
            .SingleOrDefaultAsync(x => (x.Email != null && x.Email == UserLoginData.Email) ||
                                       (x.UserName != null && x.UserName == UserLoginData.UserName));

        //Tell the admin people to check on this user
        _logger.LogWarning("The AuthUser with email {0} wasn't added. " +
                           "The matching AddNewUserInfo in the database was {1}. " +
                           "Please check the user is OK",
            UserLoginData.Email, authInfoForUser?.CreatedAtUtc.ToLongDateString() ?? "not found.");

        return status.AddError(
            $"Something went wrong. There wasn't an AuthUser with an email of {UserLoginData.Email}." +
            "Please logout and repeat the process again.");
    }
}