// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.AspNetCore.Services;
using Microsoft.Extensions.DependencyInjection;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAspNetCore;

public class TestShardingConnectionString
{
    private readonly ITestOutputHelper _output;

    public TestShardingConnectionString(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestGetAllConnectionStrings()
    {
        //SETUP
        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        services.AddTransient<ShardingConnections>();
        var serviceProvider = services.BuildServiceProvider();

        var service = serviceProvider.GetRequiredService<ShardingConnections>();

        //ATTEMPT
        var connectionNames = service.GetAllConnectionStringNames().ToArray();

        //VERIFY
        foreach (var name in connectionNames)
        {
            _output.WriteLine(name);
        }
        connectionNames.Length.ShouldEqual(3);
        connectionNames[0].ShouldEqual("AnotherConnectionString");
        connectionNames[1].ShouldEqual("UnitTestConnection");
        connectionNames[2].ShouldEqual("Version1Example4");
    }

    [Fact]
    public void TestGetNamedConnectionString()
    {
        //SETUP
        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        services.AddTransient<ShardingConnections>();
        var serviceProvider = services.BuildServiceProvider();

        var service = serviceProvider.GetRequiredService<ShardingConnections>();

        //ATTEMPT
        var connectionString = service.GetNamedConnectionString("AnotherConnectionString");

        //VERIFY
        connectionString.ShouldEqual("Server=MyServer;Database=DummyDatabase;");
    }
}