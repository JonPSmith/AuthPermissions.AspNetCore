// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ExamplesCommonCode.DownStatusCode;

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