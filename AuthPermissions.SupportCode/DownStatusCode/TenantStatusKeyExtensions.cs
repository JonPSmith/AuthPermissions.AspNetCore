// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// These extension methods can create the unique tenant value for a tenant "down" status
/// </summary>
public static class TenantStatusKeyExtensions
{
    /// <summary>
    /// Creates the unique tenant value from the user's claims
    /// </summary>
    /// <param name="tenantTypes"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public static string FormUniqueTenantValue(this TenantTypes tenantTypes, ClaimsPrincipal user)
    {
        return tenantTypes.HasFlag(TenantTypes.AddSharding)
            ? $"{user.FindFirst(PermissionConstants.DatabaseInfoNameType)?.Value}|{user.FindFirst(PermissionConstants.DataKeyClaimType)?.Value}"
            : user.FindFirst(PermissionConstants.DataKeyClaimType)?.Value;
    }

    /// <summary>
    /// This creates the unique tenant value from the basic parts: DataKey and DatabaseInfoName
    /// </summary>
    /// <param name="dataKey"></param>
    /// <param name="databaseInfoName"></param>
    /// <returns></returns>
    public static string FormUniqueTenantValue(this string dataKey, string databaseInfoName = null)
    {
        return databaseInfoName != null
            ? $"{databaseInfoName}|{dataKey}"
            : dataKey;
    }

    /// <summary>
    /// This creates the unique tenant value from the <see cref="Tenant"/> class
    /// </summary>
    /// <param name="tenant"></param>
    /// <returns></returns>
    public static string FormUniqueTenantValue(this Tenant tenant)
    {
        return tenant.GetTenantDataKey().FormUniqueTenantValue(tenant.DatabaseInfoName);
    }

    /// <summary>
    /// This reads in the <see cref="Tenant"/> using its TenantId and creates the unique tenant value 
    /// </summary>
    /// <param name="tenantAdmin"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public static async Task<string> FormedTenantCombinedKeyAsync(this IAuthTenantAdminService tenantAdmin, int tenantId)
    {
        var status = await tenantAdmin.GetTenantViaIdAsync(tenantId);
        return status.Result?.FormUniqueTenantValue();
    }

}