// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.SetupCode;

/// <summary>
/// This class is used to bulk loading tenants into the AuthP's database on startup
/// </summary>
public class BulkLoadTenantDto
{
    /// <summary>
    /// This defines a tenant in an multi-tenant application
    /// </summary>
    /// <param name="tenantName">Name of the specific tenant level. So, for hierarchical tenant you only give the name at this tenant level.</param>
    /// <param name="tenantRolesCommaDelimited">Optional: comma delimited string containing the names of Roles designed to work in this tenant. NOTE:
    ///     - If null in a hierarchical multi-tenant system, then the parent's list of tenant Roles are used
    ///     - If empty in a hierarchical multi-tenant system, then it doesn't use the parents list of tenant Role</param>
    /// <param name="childrenTenants">For hierarchical multi-tenants you provide the </param>
    public BulkLoadTenantDto(string tenantName,
        string tenantRolesCommaDelimited = null, BulkLoadTenantDto[] childrenTenants = null)
    {
        TenantName = tenantName?.Trim() ?? throw new ArgumentNullException(nameof(tenantName));
        TenantRolesCommaDelimited = tenantRolesCommaDelimited;
        ChildrenTenants = childrenTenants;
        if (ChildrenTenants != null)
            foreach (var bulkLoadTenantDto in ChildrenTenants)
            {
                bulkLoadTenantDto.Parent = this;
            }
    }

    /// <summary>
    /// Name of this specific tenant level.
    /// - For single-level tenants its the tenant name
    /// - For hierarchical multi-tenant, its the specific name of the tenant level
    ///   e.g. if you adding the shop Dress4U, the TenantName is "Dress4U" and the fullname 
    /// </summary>
    public string TenantName { get; }

    /// <summary>
    /// Optional: You can add AuthP's tenant roles via this string
    /// The Roles must have a <see cref="RoleToPermissions.RoleType"/> of <see cref="RoleTypes.TenantAutoAdd"/> or <see cref="RoleTypes.TenantAdminAdd"/>
    /// NOTE:
    /// - If null in a hierarchical multi-tenant system, then the parent's list of tenant Roles are used
    /// - If empty in a hierarchical multi-tenant system, then it doesn't use the parents list of tenant Roles
    /// </summary>
    public string TenantRolesCommaDelimited { get; internal set; }

    /// <summary>
    /// Only used in hierarchical multi-tenant apps. This array holds the children tenants from this tenant 
    /// </summary>
    public BulkLoadTenantDto[] ChildrenTenants { get; }


    //--------------------------------------------------------
    // These are used by the BulkLoadTenantsService when dealing with hierarchical data

    /// <summary>
    /// Link back to the <see cref="BulkLoadTenantDto"/> data of the parent 
    /// </summary>
    internal BulkLoadTenantDto Parent { get; set; }

    internal int CreatedTenantId { get; set; }

    internal string CreatedTenantFullName { get; set; }

    /// <summary>
    /// Useful for debug
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {

        return $"{nameof(TenantName)}: {TenantName}, {nameof(TenantRolesCommaDelimited)}: {TenantRolesCommaDelimited}, NumChildren: {ChildrenTenants?.Length ?? 0}";
    }
}