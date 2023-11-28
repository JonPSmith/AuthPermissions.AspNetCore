// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices
{
    /// <summary>
    /// Bulk load multiple tenants
    /// </summary>
    public interface IBulkLoadTenantsService
    {
        /// <summary>
        /// This allows you to add tenants to the database on startup.
        /// It gets the definition of each tenant from the <see cref="BulkLoadTenantDto"/> class
        /// </summary>
        /// <param name="tenantSetupData">If you are using a single layer then each line contains the a tenant name
        /// </param>
        /// <param name="options">The AuthPermissionsOptions to check what type of tenant setting you have</param>
        /// <returns></returns>
        Task<IStatusGeneric> AddTenantsToDatabaseAsync(List<BulkLoadTenantDto> tenantSetupData, AuthPermissionsOptions options);
    }
}