// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.SetupCode;

/// <summary>
/// This class is used for bulk loading of AuthP's Roles
/// </summary>
public class BulkLoadRolesDto
{
    /// <summary>
    /// Define a Role and the permissions in the Role: must be unique and not null
    /// </summary>
    /// <param name="roleName">Name of the Role: must be unique</param>
    /// <param name="description">Human-friendly description of what the Role provides. Can be null</param>
    /// <param name="permissionsCommaDelimited">A list of the names of the `Permissions` in this Role</param>
    /// <param name="roleType">Optional: Only used if the Role is linked to a tenant</param>
    public BulkLoadRolesDto(string roleName, string description, string permissionsCommaDelimited, RoleTypes? roleType = null)
    {
        RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        Description = description;
        RoleType = roleType ?? RoleTypes.Normal;
        PermissionsCommaDelimited = permissionsCommaDelimited;
    }

    /// <summary>
    /// Name of the Role: must be unique and not null
    /// </summary>
    public string RoleName { get; set; }
    /// <summary>
    /// Human-friendly description of what the Role provides. Can be null
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// The Type of the Role. This is only used in multi-tenant applications 
    /// </summary>
    public RoleTypes RoleType { get; set; }

    /// <summary>
    /// A list of the names of the `Permissions` in this Role
    /// The names come from your Permissions enum members
    /// </summary>
    public string PermissionsCommaDelimited { get; set; }
}