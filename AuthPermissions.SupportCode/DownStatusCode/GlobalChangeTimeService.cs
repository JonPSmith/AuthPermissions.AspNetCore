// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using Net.DistributedFileStoreCache;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This service handles the reading and writing of a DateTime to a place that all instances of the application
/// Its uses the FileStore cache to save / return the 
/// </summary>
public class GlobalChangeTimeService : IGlobalChangeTimeService
{
    /// <summary>
    /// This is the key for the "last changed" time
    /// </summary>
    public const string ChangeAtThisTimeCacheKeyName = "ChangeAtThisTime";

    private readonly IDistributedFileStoreCacheClass _fsCache;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="fsCache"></param>
    public GlobalChangeTimeService(IDistributedFileStoreCacheClass fsCache)
    {
        _fsCache = fsCache;
    }

    /// <summary>
    /// This will write a file to a global directory. The file contains the <see cref="DateTime.UtcNow"/> as a string
    /// </summary>
    /// <param name="minutesToExpiration">Optional: if the parameter > 0 the cache entry will expire after the the given minutes.
    /// This will very slightly improve performance.</param>
    public void SetGlobalChangeTimeToNowUtc(int minutesToExpiration = 0)
    {
        if (minutesToExpiration > 0)
            _fsCache.Set(ChangeAtThisTimeCacheKeyName, DateTime.UtcNow.DateTimeToTicks(), 
                new DistributedCacheEntryOptions{ AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutesToExpiration) });
        else
            _fsCache.Set(ChangeAtThisTimeCacheKeyName, DateTime.UtcNow.DateTimeToTicks());
    }

    /// <summary>
    /// This gets the cache value with the <see cref="ChangeAtThisTimeCacheKeyName"/> key returned as a DateTime
    /// If the cache value isn't found, then it returns <see cref="DateTime.MinValue"/>, which says no change has happened
    /// </summary>
    /// <returns></returns>
    public DateTime GetGlobalChangeTimeUtc()
    {
        var cachedTime = _fsCache.Get(ChangeAtThisTimeCacheKeyName);
        //If no time, then hasn't had a change yet, so provide DateTime.MinValue
        return cachedTime?.TicksToDateTimeUtc() ?? DateTime.MinValue;
    }

    /// <summary>
    /// Remove the global change entry (not in the interface)
    /// </summary>
    public void DeleteGlobalFile()
    {
        _fsCache.Remove(ChangeAtThisTimeCacheKeyName);
    }
}