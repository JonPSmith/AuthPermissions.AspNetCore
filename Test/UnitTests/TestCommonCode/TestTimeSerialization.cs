// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using ExamplesCommonCode.IdentityCookieCode;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestCommonCode;

public class TestTimeClaimsExtensions
{
    [Fact]
    public void TestDateTimeToTicks()
    {
        //SETUP
        var dateTime = new DateTime(2000, 2, 3, 4, 5, 6);

        //ATTEMPT
        var dateTimeAsString = dateTime.DateTimeToTicks();

        //VERIFY
        dateTimeAsString.ShouldEqual("630851475060000000");
    }

    [Fact]
    public void TestStringToDateTimeUtc()
    {
        //SETUP
        var dateTimeAsString = "630851475060000000";

        //ATTEMPT
        var dateTime = dateTimeAsString.TicksToDateTimeUtc();

        //VERIFY
        dateTime.ShouldEqual(new DateTime(2000, 2, 3, 4, 5, 6));
        dateTime.Kind.ShouldEqual(DateTimeKind.Utc);
    }
}