// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Net.DistributedFileStoreCache;

namespace Example4.ShopCode.CacheCode;

public static class DownForStatusExtensions
{
    //Cache key constants
    public const string DownForStatusPrefix = "DownStatus-";
    public static readonly string DownForStatusAllAppDown = $"{DownForStatusPrefix}AllAppDown";
    public static readonly string DownForStatusTenantUpdate = $"{DownForStatusPrefix}TenantId-";

    public static string FormTenantDownKey(this string dataKey) => $"{DownForStatusTenantUpdate}{dataKey}";

    /// <summary>
    /// Adds a "tenant down" status to the cache
    /// </summary>
    /// <param name="fsCache"></param>
    /// <param name="dataKey"></param>
    public static void AddTenantDownStatusCache(this IDistributedFileStoreCacheClass fsCache, string dataKey)
    {
        fsCache.Set(dataKey.FormTenantDownKey(), dataKey);
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

}