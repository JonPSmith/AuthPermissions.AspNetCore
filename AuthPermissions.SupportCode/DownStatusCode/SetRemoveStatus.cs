// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using Net.DistributedFileStoreCache;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This defines the three types of tenant "downs"
/// </summary>
public enum TenantDownVersions
{
    /// <summary>
    /// Tenant "down for update" (temporary, while change)
    /// </summary>
    Update,
    /// <summary>
    /// Manual "down" a tenant (controlled by admin user)
    /// </summary>
    ManualDown,
    /// <summary>
    /// The tenant was deleted, so you don't logged-in user to try to access it (permanent)
    /// </summary>
    Deleted
}

/// <summary>
/// This service provides methods to read, set and remove app and tenant "downs"
/// </summary>
public class SetRemoveStatus : ISetRemoveStatus
{
    private readonly IDistributedFileStoreCacheClass _fsCache;
    private readonly IAuthTenantAdminService _authTenantAdmin;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="fsCache"></param>
    /// <param name="authTenantAdmin"></param>
    public SetRemoveStatus(IDistributedFileStoreCacheClass fsCache, IAuthTenantAdminService authTenantAdmin)
    {
        _fsCache = fsCache;
        _authTenantAdmin = authTenantAdmin;
    }

    /// <summary>
    /// This returns a list of key/values that start with the down status 
    /// </summary>
    /// <returns></returns>
    public List<KeyValuePair<string, string>> GetAllDownKeyValues()
    {
        return _fsCache.GetAllKeyValues()
            .Where(x => x.Key.StartsWith(RedirectUsersViaStatusData.DownForStatusPrefix))
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value))
            .ToList();
    }

    /// <summary>
    /// This returns the messages in the appDown settings
    /// </summary>
    /// <returns></returns>
    public ManuelAppDownDto GetAppDownMessage()
    {
        return _fsCache.GetClass<ManuelAppDownDto>(RedirectUsersViaStatusData.DivertAppDown);
    }

    /// <summary>
    /// This sets the manual app down status
    /// </summary>
    /// <param name="data"></param>
    public void SetAppDown(ManuelAppDownDto data)
    {
        if (data.UserId == null)
            throw new AuthPermissionsException(
                "You must set the userId of the user who took the app down, otherwise they can't up the app");
        _fsCache.SetClass(RedirectUsersViaStatusData.DivertAppDown, data);
    }

    /// <summary>
    /// This removes any cache entry that starts with the Down Status
    /// </summary>
    /// <param name="cacheDownKey"></param>
    public void RemoveAnyDown(string cacheDownKey)
    {
        if (RedirectUsersViaStatusData.DownForStatusPrefix.StartsWith(cacheDownKey))
            throw new AuthPermissionsException("The key wasn't a status type");

        _fsCache.Remove(cacheDownKey);
    }

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
    public async Task<Func<Task>> SetTenantDownWithDelayAsync(TenantDownVersions downType, int tenantId,
        int parentId = default, int delayMs = 100)
    {
        if (tenantId == 0)
            throw new AuthPermissionsException("You must provide the tenant's id.");

        var mainKey = await _authTenantAdmin.FormedTenantCombinedKeyAsync(tenantId);
        await _fsCache.SetAsync(FormCacheKey(downType, mainKey), mainKey);
        string secondaryKey = null;
        if (parentId != default)
        { 
            secondaryKey = await _authTenantAdmin.FormedTenantCombinedKeyAsync(parentId);
            await _fsCache.SetAsync(FormCacheKey(downType, secondaryKey), secondaryKey);
        }

        await Task.Delay(delayMs);
        return () =>  RemoveTenantDownAsync(downType, mainKey, secondaryKey);
    }

    //Not used, but kept in case it might be useful 
    ///// <summary>
    ///// This removes the cache entry that was "downing" the tenant
    ///// </summary>
    ///// <param name="downType">The type of the tenant down. Its needed to create the correct key</param>
    ///// <param name="tenantId">TenantId of the tenant you want to remove from down</param>
    ///// <param name="parentId">Optional: When executing a hierarchical Move you need to provide the new parent to be downed too</param>
    ///// <exception cref="AuthPermissionsException"></exception>
    //public async Task RemoveTenantDownAsync(TenantDownVersions downType, int tenantId, int parentId = default)
    //{
    //    if (tenantId == 0)
    //        throw new AuthPermissionsException(
    //            "You must provide the tenant's id.");

    //    var mainKey = await _authTenantAdmin.FormedTenantCombinedKeyAsync(tenantId);
    //    var secondaryKey = parentId != default
    //        ? await _authTenantAdmin.FormedTenantCombinedKeyAsync(parentId)
    //        : null;
    //    await RemoveTenantDownAsync(downType, mainKey, secondaryKey);
    //}

    //-------------------------------------------------------
    // private methods

    private async Task RemoveTenantDownAsync(TenantDownVersions downType, string mainKey, string secondaryKey)
    {
        await _fsCache.RemoveAsync(FormCacheKey(downType, mainKey));
        if (secondaryKey != null)
        {
            await _fsCache.RemoveAsync(FormCacheKey(downType, secondaryKey));
        }
    }

    private string FormCacheKey(TenantDownVersions downType, string combinedKey)
    {
        switch (downType)
        {
            case TenantDownVersions.Update:
                return $"{RedirectUsersViaStatusData.DivertTenantUpdate}{combinedKey}";
            case TenantDownVersions.ManualDown:
                return $"{RedirectUsersViaStatusData.DivertTenantManuel}{combinedKey}";
            case TenantDownVersions.Deleted:
                return $"{RedirectUsersViaStatusData.DivertTenantDeleted}{combinedKey}";
            default:
                throw new ArgumentOutOfRangeException(nameof(downType), downType, null);
        }
    }
}