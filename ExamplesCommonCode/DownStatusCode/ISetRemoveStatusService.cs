// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.CommonCode;

namespace ExamplesCommonCode.DownStatusCode;

public interface ISetRemoveStatusService
{
    /// <summary>
    /// This returns a list of key/values that start with the down status 
    /// </summary>
    /// <returns></returns>
    List<KeyValuePair<string, string>> GetAllDownKeyValues();

    /// <summary>
    /// This returns the messages in the appDown settings
    /// </summary>
    /// <returns></returns>
    ManuelAppDownDto GetAppDownMessage();

    /// <summary>
    /// This sets the manual app down status
    /// </summary>
    /// <param name="data"></param>
    void SetAppDown(ManuelAppDownDto data);

    /// <summary>
    /// This removes any cache entry that starts with the Down Status
    /// </summary>
    /// <param name="cacheDownKey"></param>
    void RemoveAnyDown(string cacheDownKey);

    /// <summary>
    /// This sets a tenant down and then delays so that any current accesses to the tenant should have finished.
    /// This also returns the method to remove the tenant down
    /// </summary>
    /// <param name="downType">The type of the tenant down - this defines what page/message the user sees while the tenant is down</param>
    /// <param name="tenantId">TenantId of the tenant to take down</param>
    /// <param name="parentId">Optional: When executing a hierarchical Move you need to provide the new parent to be downed too</param>
    /// <param name="delayMs">Delay once the cache down has been set. Defaults to 100 ms</param>
    /// <returns>Returns the code to remove the tenant down status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    Task<Func<Task>> SetTenantDownWithDelayAsync(TenantDownVersions downType, int tenantId, int parentId = default,
        int delayMs = 100);

    ///// <summary>
    ///// This removes the cache entry that was "downing" the tenant
    ///// </summary>
    ///// <param name="downType">The type of the tenant down. Its needed to create the correct key</param>
    ///// <param name="tenantId">TenantId of the tenant you want to remove from down</param>
    ///// <param name="parentId">Optional: When executing a hierarchical Move you need to provide the new parent to be downed too</param>
    ///// <exception cref="AuthPermissionsException"></exception>
    //Task RemoveTenantDownAsync(TenantDownVersions downType, int tenantId, int parentId = default);
}