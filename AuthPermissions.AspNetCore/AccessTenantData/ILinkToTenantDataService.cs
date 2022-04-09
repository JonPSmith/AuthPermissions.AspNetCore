// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AspNetCore.AccessTenantData.Services;
using AuthPermissions.BaseCode.CommonCode;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.AccessTenantData;

/// <summary>
/// This define the link to tenant data services
/// </summary>
public interface ILinkToTenantDataService
{
    /// <summary>
    /// This will change the DataKey to a different tenant than the current user's DataKey
    /// This does this by creating a cookie that contains a DataKey that will replace the current user's DataKey claim
    /// </summary>
    /// <param name="currentUserId">Id of the current user. Used to check that user type matches the </param>
    /// <param name="tenantId">The primary key of the Tenant the user wants to access</param>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<IStatusGeneric> StartLinkingToTenantDataAsync(string currentUserId, int tenantId);

    /// <summary>
    /// This stops the current user's DataKey being set by the <see cref="LinkToTenantDataService.StartLinkingToTenantDataAsync"/> method.
    /// It simply deletes the <see cref="AccessTenantDataCookie"/>
    /// </summary>
    void StopLinkingToTenant();

    /// <summary>
    /// This gets the DataKey from the <see cref="AccessTenantDataCookie"/>
    /// If there no cookie it returns null
    /// </summary>
    /// <returns></returns>
    string GetDataKeyOfLinkedTenant();

    /// <summary>
    /// This gets the DataKey and ConnectionName from the <see cref="AccessTenantDataCookie"/>
    /// If there no cookie it returns null for both properties
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    (string dataKey, string connectionName) GetShardingDataOfLinkedTenant();

    /// <summary>
    /// This gets the TenantFullName of the tenant that the <see cref="AccessTenantDataCookie"/> contains
    /// If there no cookie it returns null
    /// </summary>
    /// <returns></returns>
    string GetNameOfLinkedTenant();
}