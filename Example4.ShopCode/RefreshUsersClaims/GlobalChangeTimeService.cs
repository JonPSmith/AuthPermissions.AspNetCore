// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using ExamplesCommonCode.IdentityCookieCode;
using Microsoft.AspNetCore.Hosting;

namespace Example4.ShopCode.RefreshUsersClaims;

/// <summary>
/// This service handles the reading and writing of a DateTime to a place that all instances of the application
/// Its uses <see cref="PoorMansGlobalCache"/> which uses a File - that works but a common cache like Redis would be perform better
/// </summary>
public class GlobalChangeTimeService : IGlobalChangeTimeService
{
    private const string ChangedTimeFileName = "GlobalChangedTimeUtc";

    private readonly PoorMansGlobalCache _globalCache;

    public GlobalChangeTimeService(IWebHostEnvironment environment)
    {
        _globalCache = new PoorMansGlobalCache(environment);
    }

    /// <summary>
    /// This will write a file to a global directory. The file contains the <see cref="DateTime.UtcNow"/> as a string
    /// </summary>
    public void SetGlobalChangeTimeToNowUtc()
    {
        _globalCache.Set(ChangedTimeFileName, DateTime.UtcNow.DateTimeToStringUtc());
    }

    /// <summary>
    /// This reads the File in a global directory and returns the DateTime of the in the file
    /// If no file is found, then it returns <see cref="DateTime.MinValue"/>, which says no change has happened
    /// </summary>
    /// <returns></returns>
    public DateTime GetGlobalChangeTimeUtc()
    {
        var cachedTime = _globalCache.Get(ChangedTimeFileName);
        //If no time, then hasn't had a change yet, so provide DateTime.MinValue
        return cachedTime?.StringToDateTimeUtc() ?? DateTime.MinValue;
    }

    public void DeleteGlobalFile()
    {
        _globalCache.Remove(ChangedTimeFileName);
    }
}