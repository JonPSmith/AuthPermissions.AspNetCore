// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This is used to select a tenant to manually "down" a tenant 
/// </summary>
public class ManuelTenantDownDto
{
    /// <summary>
    /// Id of tenant to down
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// This contains a list of tenants, with the Key being the tenantId and the Value the tenantName
    /// </summary>
    public List<KeyValuePair<int, string>> ListOfTenants { get; private set; }

    /// <summary>
    /// this will set up the list of tenants to select from
    /// </summary>
    /// <param name="tenantAdminService"></param>
    /// <returns></returns>
    public static async Task<ManuelTenantDownDto> SetupListOfTenantsAsync(IAuthTenantAdminService tenantAdminService)
    {
        return new ManuelTenantDownDto
        {
            ListOfTenants = await tenantAdminService.QueryTenants()
                .OrderBy(x => x.TenantFullName)
                .Select(x => new KeyValuePair<int, string>(x.TenantId, x.TenantFullName))
                .ToListAsync()
        };
    }
}