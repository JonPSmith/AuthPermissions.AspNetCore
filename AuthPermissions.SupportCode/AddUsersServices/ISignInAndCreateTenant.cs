// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This interface defines the service to implement AuthP's "sign up" feature, which allows a new user to automatically
/// create a new tenant and becomes the tenant admin user for this new tenant. 
/// </summary>
public interface ISignInAndCreateTenant
{
    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant.
    /// This method creates the tenant using the information provides by the user and the
    /// <see cref="MultiTenantVersionData"/> for this application.
    /// </summary>
    /// <param name="dto">The data provided by the user and extra data, like the version, from the sign in</param>
    /// <param name="versionData">This contains the application's setup of your tenants, including different versions.</param>
    /// <returns>Status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<IStatusGeneric> AddUserAndNewTenantAsync(AddNewTenantDto dto, MultiTenantVersionData versionData);
}