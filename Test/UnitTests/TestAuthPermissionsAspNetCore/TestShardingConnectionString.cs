// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Test.TestHelpers;
using TestSupport.EfHelpers;
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
        var serviceProvider = services.BuildServiceProvider();

        var snapShot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
        var service = new ShardingConnections(snapShot, null);

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
        var serviceProvider = services.BuildServiceProvider();

        var snapShot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
        var service = new ShardingConnections(snapShot, null);

        //ATTEMPT
        var connectionString = service.GetNamedConnectionString("AnotherConnectionString");

        //VERIFY
        connectionString.ShouldEqual("Server=MyServer;Database=DummyDatabase;");
    }

    [Fact]
    public async Task TestQueryTenantsSingle()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenant1 = Tenant.CreateSingleTenant("Tenant1").Result;
        tenant1.UpdateShardingState("AnotherConnectionString", false);
        var tenant2 = Tenant.CreateSingleTenant("Tenant3").Result;
        tenant2.UpdateShardingState("AnotherConnectionString", false);
        var tenant3 = Tenant.CreateSingleTenant("Tenant2").Result;
        tenant3.UpdateShardingState("UnitTestConnection", false);
        context.AddRange(tenant1, tenant2, tenant3);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        var serviceProvider = services.BuildServiceProvider();

        var snapShot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
        var service = new ShardingConnections(snapShot, context);

        //ATTEMPT
        var keyPairs = await service.GetConnectionStringsWithTenantNamesAsync();

        //VERIFY
        keyPairs.ShouldEqual(new List<(string connectionName, List<string> tenantNames)>
        {
            ("AnotherConnectionString", new List<string>{"Tenant1", "Tenant3"}),
            ("UnitTestConnection", new List<string>{ "Tenant2"}),
            ("Version1Example4", new List<string>())
        });
    }
}