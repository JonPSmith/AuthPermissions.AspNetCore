// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using ExamplesCommonCode.IdentityCookieCode;
using Microsoft.AspNetCore.Hosting;
using Net.DistributedFileStoreCache;

namespace Example4.ShopCode.RefreshUsersClaims;

/// <summary>
/// This service handles the reading and writing of a DateTime to a place that all instances of the application
/// Its uses the FileStore cache to save / return the 
/// </summary>
public class GlobalChangeTimeService : IGlobalChangeTimeService
{
    public const string ChangeAtThisTimeCacheKeyName = "ChangeAtThisTime";

    private readonly IDistributedFileStoreCacheClass _fsCache;

    public GlobalChangeTimeService(IDistributedFileStoreCacheClass fsCache)
    {
        _fsCache = fsCache;
    }

    /// <summary>
    /// This will write a file to a global directory. The file contains the <see cref="DateTime.UtcNow"/> as a string
    /// </summary>
    public void SetGlobalChangeTimeToNowUtc()
    {
        _fsCache.Set(ChangeAtThisTimeCacheKeyName, DateTime.UtcNow.DateTimeToTicks());
    }

    /// <summary>
    /// This reads the File in a global directory and returns the DateTime of the in the file
    /// If no file is found, then it returns <see cref="DateTime.MinValue"/>, which says no change has happened
    /// </summary>
    /// <returns></returns>
    public DateTime GetGlobalChangeTimeUtc()
    {
        var cachedTime = _fsCache.Get(ChangeAtThisTimeCacheKeyName);
        //If no time, then hasn't had a change yet, so provide DateTime.MinValue
        return cachedTime?.TicksToDateTimeUtc() ?? DateTime.MinValue;
    }

    public void DeleteGlobalFile()
    {
        _fsCache.Remove(ChangeAtThisTimeCacheKeyName);
    }
}