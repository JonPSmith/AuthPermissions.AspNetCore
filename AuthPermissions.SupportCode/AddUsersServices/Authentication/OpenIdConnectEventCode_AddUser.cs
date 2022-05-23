// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

/// <summary>
/// This class contains the code to run inside an OpenIDConnect <see cref="OpenIdConnectEvents.OnTokenValidated"/> event
/// Its job is to find a AuthUser linked to the userId of the user that is logging in.
/// If no AuthUser is found, then it checks to see if a new AuthUser should be added via data added to the AuthP database
/// </summary>
public class OpenIdConnectEventCode_AddUser : NonRegisterAuthenticationEventCode<TokenValidatedContext>
{
    /// <summary>
    /// This method adds an AuthUser when creating a new User via OpenIdConnect using a <see cref="NonRegisterAddUserManager"/>
    /// This method should be added to the <see cref="OpenIdConnectEvents.OnTokenValidated"/> event code.
    /// run within the authentication event that which
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
    public override async Task<List<Claim>> ManageAuthUserPartAsync(TokenValidatedContext eventContext, 
        string userId, string email, string userName = null)
    {
        var authPUserService =
            eventContext.HttpContext.RequestServices.GetRequiredService<IAuthUsersAdminService>();
        var findStatus = await authPUserService.FindAuthUserByUserIdAsync(userId);
        var authFound = findStatus.Result != null;
        if (!authFound)
        {
            //There is no AuthUser linked to the userId, so we see if this user should be add a new user

            var authContext =
                eventContext.HttpContext.RequestServices.GetRequiredService<AuthPermissionsDbContext>();
            var authUserInfo = await GetUserAuthInfoFromTheDatabaseAsync(authContext, email, userName);
            if (authUserInfo != null)
            {
                //Yes, there information for creating a new AuthUser

                var rolesList = authUserInfo.RolesNamesCommaDelimited?.Split(',').ToList() ?? new List<string>();
                var tenantName = authUserInfo.TenantId == null
                    ? null
                    : (await authContext.Tenants.SingleOrDefaultAsync(x => x.TenantId == authUserInfo.TenantId))
                        ?.TenantFullName;

                var createStatus = await authPUserService.AddNewUserAsync(userId, email, userName, rolesList, tenantName);
                createStatus.IfErrorsTurnToException();
                authFound = true;
            }
        }

        if (!authFound)
            return null;

        //We have an AuthUser linked to this login, so return the claims
        var claimsCalculator =
            eventContext.HttpContext.RequestServices.GetRequiredService<IClaimsCalculator>();
        return await claimsCalculator.GetClaimsForAuthUserAsync(userId);

    }
}