// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// Interface for the service that set / get the time of the last registered change
/// </summary>
public interface IGlobalChangeTimeService
{
    /// <summary>
    /// This will write a file to a global directory. The file contains the <see cref="DateTime.UtcNow"/> as a string
    /// </summary>
    /// <param name="minutesToExpiration">Optional: if the parameter > 0 the cache entry will expire after the the given minutes.
    /// This will very slightly improve performance.</param>
    public void SetGlobalChangeTimeToNowUtc(int minutesToExpiration = 0);

    /// <summary>
    /// This gets the cache value with the global change key returned as a DateTime
    /// If the cache value isn't found, then it returns <see cref="DateTime.MinValue"/>, which says no change has happened
    /// </summary>
    /// <returns></returns>
    DateTime GetGlobalChangeTimeUtc();
}