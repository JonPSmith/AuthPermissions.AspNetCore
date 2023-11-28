// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This is holds the data required for creating different versions of your multi tenant application
/// You fill this in and provide it to the <see cref="SignInAndCreateTenant"/> service
/// </summary>
public class MultiTenantVersionData
{
    /// <summary>
    /// The dictionary key should contain the name of each version,
    /// and each value contains the Tenant Roles to be added to this version of a Tenant.
    /// It null, then no Tenant Roles are added to the tenant
    /// </summary>
    public Dictionary<string, List<string>> TenantRolesForEachVersion { get; set; }

    /// <summary>
    /// This holds the Roles of a tenant admin for each version,
    /// i.e. they can manage the users in their tenant including the
    /// ability to invite a new user to your tenant.
    /// </summary>
    public Dictionary<string, List<string>> TenantAdminRoles { get; set; }

    /// <summary>
    /// If <see cref="TenantTypes.AddSharding"/> is on, then you can define which
    /// tenant versions have their own DB.
    /// If this property isn't null, then it will override the <see cref="AddNewTenantDto"/>.<see cref="AddNewTenantDto.HasOwnDb"/> property
    /// </summary>
    public Dictionary<string, bool?> HasOwnDbForEachVersion { get; set; }
}