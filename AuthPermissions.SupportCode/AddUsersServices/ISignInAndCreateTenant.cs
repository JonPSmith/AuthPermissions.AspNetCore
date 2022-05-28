// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.ShardingServices;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This interface defines the service to implement AuthP's "sign up" feature, which allows a new user to 
/// create a new tenant automatically and then register the user to  new tenant. 
/// </summary>
public interface ISignInAndCreateTenant
{
    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant,
    /// where there is only version of the tenant. It also creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="userInfo">This contains the information for the new user. Both the user login and any AuthP Roles, etc.</param>
    /// <param name="tenantName">This is the name for the new tenant - it will check the name is not already used</param>
    /// <param name="hasOwnDb">If the app is sharding, then must be set to true of tenant having its own db, of false for shared db</param>
    /// <param name="region">Optional: This is used when you have database servers geographically spread.
    /// It helps the <see cref="IGetDatabaseForNewTenant"/> service to pick the right server/database.</param>
    /// <returns>status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<IStatusGeneric> SignUpNewTenantAsync(AddUserDataDto userInfo, string tenantName, bool? hasOwnDb = null,
        string region = null);


    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant, with versioning.
    /// This method creates the tenant using the <see cref="MultiTenantVersionData"/> for this application
    /// with backup version information provides by the user.
    /// At the same time is creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="signUpInfo">The data provided by the user and extra data, like the version, from the sign in</param>
    /// <param name="versionData">This contains the application's setup of your tenants, including different versions.</param>
    /// <returns>Status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<IStatusGeneric> SignUpNewTenantWithVersionAsync(AddNewTenantDto signUpInfo, MultiTenantVersionData versionData);
}