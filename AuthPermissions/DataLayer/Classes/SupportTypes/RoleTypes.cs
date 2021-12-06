// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.DataLayer.Classes.SupportTypes;

/// <summary>
/// This enum defines the different types of Roles
/// </summary>
public enum RoleTypes : byte
{
    /// <summary>
    /// A Role that can be assigned to any any user 
    /// </summary>
    Normal = 0,
    /// <summary>
    /// A Role that is assigned to an Tenant and is automatically included in the calculation of the user's Permissions
    /// </summary>
    TenantAutoAdd = 50,
    /// <summary>
    /// A Role that is assigned to an Tenant which an admin can assign to a user's list of Roles
    /// </summary>
    TenantAdminAdd = 60,
    /// <summary>
    /// This Role is hidden from any AuthP user than is linked to a Tenant
    /// The <see cref="HiddenFromTenant"/> RoleType is automatically if a Permission in the Role has the
    /// "AutoGenerateFilter = true" parameter in the Permission member's DataDisplay attribute.
    /// A RoleType of a Role can also manually set to this setting 
    /// </summary>
    HiddenFromTenant = 100,
}