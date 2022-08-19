// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Net.DistributedFileStoreCache;

namespace ExamplesCommonCode.DownStatusCode;

public static class AppStatusExtensions
{
    //Cache key constants
    public const string DownForStatusPrefix = "AppStatus-";
    public static readonly string DownForStatusAllAppDown = $"{DownForStatusPrefix}AllAppDown";
    public static readonly string StatusTenantPrefix = $"{DownForStatusPrefix}Tenant";
    public static readonly string DownForStatusTenantUpdate = $"{StatusTenantPrefix}Down-";
    public static readonly string DeletedTenantStatus = $"{StatusTenantPrefix}Deleted-";

    public static string FormTenantDownKey(this string dataKey) => $"{DownForStatusTenantUpdate}{dataKey}";
    public static string FormTenantDeletedKey(this string dataKey) => $"{DeletedTenantStatus}{dataKey}";

    /// <summary>
    /// Adds a "tenant down" status to the cache
    /// </summary>
    /// <param name="fsCache"></param>
    /// <param name="dataKey"></param>
    public static async Task AddTenantDownStatusCacheAndWaitAsync(this IDistributedFileStoreCacheClass fsCache, string dataKey)
    {
        fsCache.Set(dataKey.FormTenantDownKey(), dataKey);
        await Task.Delay(100); //we wait 100 milliseconds to be sure the that current accesses have finished
    }

    /// <summary>
    /// Remove "tenant down" status from the cache
    /// </summary>
    /// <param name="fsCache"></param>
    /// <param name="dataKey"></param>
    public static void RemoveTenantDownStatusCache(this IDistributedFileStoreCacheClass fsCache, string dataKey)
    {
        fsCache.Remove(dataKey.FormTenantDownKey());
    }

    /// <summary>
    /// Adds a "tenant deleted" status to the cache. This means that any user with this DataKey is stopped from accessing the app
    /// </summary>
    /// <param name="fsCache"></param>
    /// <param name="dataKey"></param>
    public static async Task AddTenantDeletedStatusCacheAndWaitAsync(this IDistributedFileStoreCacheClass fsCache, string dataKey)
    {
        fsCache.Set(dataKey.FormTenantDeletedKey(), dataKey);
        await Task.Delay(100); //we wait 100 milliseconds to be sure the that current accesses have finished
    }

    /// <summary>
    /// Removes a "tenant deleted" status to the cache if the delete service returned errors
    /// </summary>
    /// <param name="fsCache"></param>
    /// <param name="dataKey"></param>
    public static void RemoveDeletedStatusCache(this IDistributedFileStoreCacheClass fsCache, string dataKey)
    {
        fsCache.Remove(dataKey.FormTenantDeletedKey());
    }

}