// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Net.DistributedFileStoreCache;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.DataLayer.Classes;
using ExamplesCommonCode.DownStatusCode;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamplesCommonCode;

public class TestSetRemoveStatusService
{
    private readonly ITestOutputHelper _output;
    private readonly IDistributedFileStoreCacheClass _fsCache;

    public TestSetRemoveStatusService(ITestOutputHelper output)
    {
        _output = output;
        _fsCache = new StubFileStoreCacheClass(); //this clears the cache fro each test
    }

    private Tenant TestTenant { get; set; }

    [Fact]
    public async Task TestUserThatSetAllDownNotRedirected()
    {
        //SETUP
        var removeService = new SetRemoveStatusService(_fsCache, )

        //ATTEMPT


        //VERIFY

    }
}