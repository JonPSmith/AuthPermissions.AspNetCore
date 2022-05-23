// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

public abstract class NonRegisterAuthenticationEventCode<TEventContext> 
    where TEventContext : class
{
    /// <summary>
    /// You need to implement this method which will run within the authentication event that which
    /// says the login was valid and the <see cref="ClaimsPrincipal"/> is being created.
    /// In this case you should the following
    /// 1. Check if there an existing AuthUser with the given userId
    ///    1.a If there isn't then you look for a <see cref="AddNewUserInfo"/> with the email (or userName) of this user
    ///        1.a.1 If there is an entry, you create a new AuthUser with the Roles / TenantId
    /// 2. If an AuthUser has been found, or a new AuthUser has been created, then you return the claims 
    /// </summary>
    /// <param name="eventContext"></param>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    public abstract Task<List<Claim>> ManageAuthUserPartAsync(TEventContext eventContext, string userId, string email, string userName = null);


    /// <summary>
    /// This is used within the authentication handler event to obtain the
    /// AuthUser Roles / tenant settings when creating an AuthUser
    /// </summary>
    /// <param name="authPContext">An instance of the AuthP DbContext</param>
    /// <param name="email">email of the user we are looking for. Can be null</param>
    /// <param name="userName">userName of the user we are looking for. Can bu null</param>
    /// <returns></returns>
    protected async Task<AddNewUserInfo> GetUserAuthPInfoFromTheDatabaseAsync(
        AuthPermissionsDbContext authPContext, string email, string userName = null)
    {
        if (email == null && userName == null)
            throw new AuthPermissionsException("Both email and userName can be null.");

        var userInfo = await authPContext.AddNewUserInfos
            .SingleOrDefaultAsync(x => (x.Email != null && x.Email == email.ToLower()) ||
                                       (x.UserName != null && x.UserName == userName));
        if (userInfo != null)
        {
            authPContext.Remove(userInfo);
        }

        return userInfo;
    }
}