// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.AdminCode;

namespace ExamplesCommonCode.DownStatusCode;

public static class TenantStatusKeyExtensions
{
    public static string FormedTenantCombinedKey(this TenantTypes tenantTypes, ClaimsPrincipal user)
    {
        return tenantTypes.HasFlag(TenantTypes.AddSharding)
            ? $"{user.FindFirst(PermissionConstants.DatabaseInfoNameType)?.Value}|{user.FindFirst(PermissionConstants.DataKeyClaimType)?.Value}"
            : user.FindFirst(PermissionConstants.DataKeyClaimType)?.Value;
    }

    public static string FormedTenantCombinedKey(this string dataKey, string databaseInfoName = null)
    {
        return databaseInfoName != null
            ? $"{databaseInfoName}|{dataKey}"
            : dataKey;
    }

    public static string FormedTenantCombinedKey(this Tenant tenant)
    {
        return tenant.GetTenantDataKey().FormedTenantCombinedKey(tenant.DatabaseInfoName);
    }

    public static async Task<string> FormedTenantCombinedKeyAsync(this IAuthTenantAdminService tenantAdmin, int tenantId)
    {
        var status = await tenantAdmin.GetTenantViaIdAsync(tenantId);
        return status.Result?.FormedTenantCombinedKey();
    }

}