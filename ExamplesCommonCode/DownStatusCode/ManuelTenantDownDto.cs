// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.EntityFrameworkCore;

namespace ExamplesCommonCode.DownStatusCode;

public class ManuelTenantDownDto
{
    /// <summary>
    /// Id of the user that set the "all down" status
    /// </summary>
    public string DataKey { get; set; }
    /// <summary>
    /// Optional message
    /// </summary>

    /// <summary>
    /// This contains a list of tenants, with the Key being the DataKey and the Value the tenantName
    /// </summary>
    public List<KeyValuePair<string, string>> ListOfTenants { get; private set; }

    public static async Task<ManuelTenantDownDto> SetupListOfTenantsAsync(IAuthTenantAdminService tenantAdminService)
    {
        return new ManuelTenantDownDto
        {
            ListOfTenants = await tenantAdminService.QueryTenants()
                .OrderBy(x => x.TenantFullName)
                .Select(x => new KeyValuePair<string, string>(x.GetTenantDataKey(), x.TenantFullName))
                .ToListAsync()
        };
    }
}