// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.


using System;
using Example4.ShopCode.RefreshUsersClaims;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples;

public class TestPoorMansGlobalChangeTimeService
{
    private readonly ITestOutputHelper _output;

    public TestPoorMansGlobalChangeTimeService(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestSetGlobalChangeTimeToNowUtc_And_GetGlobalChangeTimeUtc_NoFile()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new PoorMansGlobalChangeTimeService(stubEnv);
        service.DeleteGlobalFile();

        //ATTEMPT
        using(new TimeThings(_output, "write"))
            service.SetGlobalChangeTimeToNowUtc();

        //VERIFY
        using (new TimeThings(_output, "read"))
            service.GetGlobalChangeTimeUtc().ShouldBeInRange(DateTime.UtcNow.AddMilliseconds(-200), DateTime.UtcNow);
    }

    [Fact]
    public void TestSetGlobalChangeTimeToNowUtc_And_GetGlobalChangeTimeUtc_ExistingFile()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new PoorMansGlobalChangeTimeService(stubEnv);
        service.SetGlobalChangeTimeToNowUtc();

        //ATTEMPT
        using (new TimeThings(_output, "write"))
            service.SetGlobalChangeTimeToNowUtc();

        //VERIFY
        using (new TimeThings(_output, "read"))
            service.GetGlobalChangeTimeUtc().ShouldBeInRange(DateTime.UtcNow.AddMilliseconds(-200), DateTime.UtcNow);
    }

    [Fact]
    public void TestGetGlobalChangeTimeUtc_NoFile()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new PoorMansGlobalChangeTimeService(stubEnv);
        service.DeleteGlobalFile();

        //ATTEMPT
        var dateTime = service.GetGlobalChangeTimeUtc();

        //VERIFY
        dateTime.ShouldEqual(DateTime.MinValue);
    }

    [Fact]
    public void TestGetGlobalChangeTimeUtc_Performance()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new PoorMansGlobalChangeTimeService(stubEnv);
        service.SetGlobalChangeTimeToNowUtc();

        //ATTEMPT
        for (int i = 0; i < 10; i++)
        {
            using (new TimeThings(_output, "write"))
                service.GetGlobalChangeTimeUtc();
        }

        //VERIFY
    }
}