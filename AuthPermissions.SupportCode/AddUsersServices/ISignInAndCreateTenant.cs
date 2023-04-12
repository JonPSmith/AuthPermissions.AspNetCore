// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
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
    /// <param name="newUser">The information for the new user that is signing in</param>
    /// <param name="tenantData">The information for how the new tenant should be created</param>
    /// <returns>status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<IStatusGeneric> SignUpNewTenantAsync(AddNewUserDto newUser, AddNewTenantDto tenantData);

    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant, with versioning.
    /// This method creates the tenant using the <see cref="MultiTenantVersionData"/> for this application
    /// with backup version information provides by the user.
    /// At the same time is creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="newUser">The information for the new user that is signing in</param>
    /// <param name="tenantData">The information for how the new tenant should be created</param>
    /// <param name="versionData">This contains the application's setup of your tenants, including different versions.</param>
    /// <returns>Status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<IStatusGeneric<AddNewUserDto>> SignUpNewTenantWithVersionAsync(AddNewUserDto newUser,
        AddNewTenantDto tenantData, MultiTenantVersionData versionData);
}