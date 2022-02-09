// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using ExamplesCommonCode.IdentityCookieCode;

namespace Test.TestHelpers;

public class StubGlobalChangeTimeService : IGlobalChangeTimeService
{
    public int NumTimesCalled { get; set; }

    public void SetGlobalChangeTimeToNowUtc()
    {
        NumTimesCalled += 1;
    }

    public DateTime GetGlobalChangeTimeUtc()
    {
        throw new NotImplementedException();
    }
}