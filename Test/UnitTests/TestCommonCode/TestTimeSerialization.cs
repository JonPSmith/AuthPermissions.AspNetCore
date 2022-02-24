// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using ExamplesCommonCode.IdentityCookieCode;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestCommonCode;

public class TestTimeClaimsExtensions
{
    [Fact]
    public void TestDateTimeToString()
    {
        //SETUP
        var dateTime = new DateTime(2000, 2, 3, 4, 5, 6);

        //ATTEMPT
        var dateTimeAsString = dateTime.DateTimeToStringUtc();

        //VERIFY
        dateTimeAsString.ShouldEqual("2000-02-03T04:05:06.0000000Z");
    }

    [Fact]
    public void TestStringToDateTimeUtc()
    {
        //SETUP
        var dateTimeAsString = "2000-02-03T04:05:06.0000000Z";

        //ATTEMPT
        var dateTime = dateTimeAsString.StringToDateTimeUtc();

        //VERIFY
        dateTime.ShouldEqual(new DateTime(2000, 2, 3, 4, 5, 6));
        dateTime.Kind.ShouldEqual(DateTimeKind.Utc);
    }



}