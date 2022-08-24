// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using ExamplesCommonCode.DownStatusCode;

namespace Test.StubClasses;

public class StubGlobalChangeTimeService : IGlobalChangeTimeService
{
    public int NumTimesCalled { get; set; }

    /// <summary>
    /// This will write a file to a global directory. The file contains the <see cref="DateTime.UtcNow"/> as a string
    /// </summary>
    /// <param name="minutesToExpiration">Optional: if the parameter > 0 the cache entry will expire after the the given minutes.
    /// This will very slightly improve performance.</param>
    public void SetGlobalChangeTimeToNowUtc(int minutesToExpiration = 0)
    {
        NumTimesCalled += 1;
    }

    public DateTime GetGlobalChangeTimeUtc()
    {
        throw new NotImplementedException();
    }
}