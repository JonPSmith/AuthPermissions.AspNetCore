// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAspNetCore;

public class TestShardingConnectionString
{
    private readonly ITestOutputHelper _output;
    private readonly IOptionsSnapshot<ConnectionStringsOption> _connectSnapshot;
    private readonly IOptionsSnapshot<ShardingSettingsOption> _shardingSnapshot;

    public TestShardingConnectionString(ITestOutputHelper output)
    {
        _output = output;

        var config = AppSettings.GetConfiguration("..\\Test\\TestData", "combinedshardingsettings.json");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        services.Configure<ShardingSettingsOption>(config);
        var serviceProvider = services.BuildServiceProvider();

        _connectSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
        _shardingSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ShardingSettingsOption>>();
    }

    private AuthPermissionsOptions FormAuthOptionsForSharding(bool sqlServer = true)
    {
        var options = new AuthPermissionsOptions
        {
            SecondPartOfShardingFile = "Test",
            InternalData =
            {
                AuthPDatabaseType = sqlServer ? AuthPDatabaseTypes.SqlServer : AuthPDatabaseTypes.Postgres
            }
        };
        return options;
    }

    [Fact]
    public void TestDefaultShardingDatabaseData()
    {
        //SETUP

        //ATTEMPT
        var databaseDefault = DatabaseInformation.FormDefaultDatabaseInfo(FormAuthOptionsForSharding());

        //VERIFY
        databaseDefault.Name.ShouldEqual("Default Database");
        databaseDefault.DatabaseName.ShouldBeNull();
        databaseDefault.ConnectionName.ShouldEqual("DefaultConnection");
        databaseDefault.DatabaseType.ShouldEqual("SqlServer");
    }

    [Fact]
    public void TestGetAllConnectionStrings()
    {
        //SETUP
        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, null, FormAuthOptionsForSharding());

        //ATTEMPT
        var databaseData = service.GetAllPossibleShardingData();

        //VERIFY
        foreach (var data in databaseData)
        {
            _output.WriteLine(data.ToString());
        }
        databaseData.Count.ShouldEqual(4);
        databaseData[0].Name.ShouldEqual("Default Database");
        databaseData[1].Name.ShouldEqual("Another");
        databaseData[2].Name.ShouldEqual("Bad: No DatabaseName");
        databaseData[3].Name.ShouldEqual("Special Postgres");
    }

    [Theory]
    [InlineData("DefaultConnection", true)]
    [InlineData("PostgresConnection", false)]
    public void TestFormingConnectionString(string connectionName, bool isValid)
    {
        //SETUP
        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, null, FormAuthOptionsForSharding());
        
        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            Name = "Test",
            DatabaseName = "TestDb",
            ConnectionName = connectionName,
            DatabaseType = "SqlServer"
        };
        var status = service.TestFormingConnectionString(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? "success" : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
    }

    [Fact]
    public void TestGetNamedConnectionStringSqlServer()
    {
        //SETUP
        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, null, FormAuthOptionsForSharding());

        //ATTEMPT
        var connectionString = service.FormConnectionString("Another");

        //VERIFY
        connectionString.ShouldEqual("Data Source=MyServer;Initial Catalog=AnotherDatabase");
    }

    [Fact]
    public void TestGetNamedConnectionStringSqlServer_NoDatabaseName()
    {
        //SETUP
        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, null, FormAuthOptionsForSharding());

        //ATTEMPT
        var ex = Assert.Throws<AuthPermissionsException>( () => service.FormConnectionString("Bad: No DatabaseName"));

        //VERIFY
        ex.Message.ShouldEqual("The DatabaseName can't be null or empty when the connection string doesn't have a database defined.");
    }

    [Fact]
    public void TestGetNamedConnectionStringDefaultDatabase()
    {
        //SETUP
        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, null, FormAuthOptionsForSharding());

        //ATTEMPT
        var connectionString = service.FormConnectionString("Default Database");

        //VERIFY
        connectionString.ShouldEqual("Server=(localdb)\\mssqllocaldb;Database=AuthPermissions-Test;Trusted_Connection=True;MultipleActiveResultSets=true");
    }

    [Fact]
    public void TestGetNamedConnectionStringPostgres()
    {
        //SETUP
        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, null, FormAuthOptionsForSharding());

        //ATTEMPT
        var connectionString = service.FormConnectionString("Special Postgres");

        //VERIFY
        connectionString.ShouldEqual("Host=127.0.0.1;Database=MyDatabase;Username=postgres;Password=LetMeIn");
    }



    [Fact]
    public async Task TestQueryTenantsSingle()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenant1 = Tenant.CreateSingleTenant("Tenant1").Result;
        tenant1.UpdateShardingState("Default Database", false);
        var tenant2 = Tenant.CreateSingleTenant("Tenant3").Result;
        tenant2.UpdateShardingState("Default Database", false);
        var tenant3 = Tenant.CreateSingleTenant("Tenant2").Result;
        tenant3.UpdateShardingState("Another", false);
        context.AddRange(tenant1, tenant2, tenant3);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));

        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, context, FormAuthOptionsForSharding());

        //ATTEMPT
        var keyPairs = await service.GetDatabaseInfoNamesWithTenantNamesAsync();

        //VERIFY
        keyPairs.ShouldEqual(new List<(string databaseName, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Default Database", false, new List<string>{"Tenant1", "Tenant3"}),
            ("Another", false, new List<string>{ "Tenant2"}),
            ("Bad: No DatabaseName", null, new List<string>()),
            ("Special Postgres", null, new List<string>())
        });
    }

    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestQueryTenantsSingleDefaultConnectionName(bool addTenantDefaultDatabase)
    {
        //This checks that the Default DatabaseInfoName always returns a HasOwnDb of false.
        //That's because that database contains the AuthP data as well, so sharding database would have other data with it

        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        if (addTenantDefaultDatabase)
        {
            var tenant1 = Tenant.CreateSingleTenant("Tenant1").Result;
            tenant1.UpdateShardingState(FormAuthOptionsForSharding().ShardingDefaultDatabaseInfoName, true);
            context.Add(tenant1);
        }
        var tenant2 = Tenant.CreateSingleTenant("Tenant2").Result;
        tenant2.UpdateShardingState("Another", false);
        context.Add(tenant2);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));

        var service = new ShardingConnections(_connectSnapshot, _shardingSnapshot, context, FormAuthOptionsForSharding());

        //ATTEMPT
        var keyPairs = await service.GetDatabaseInfoNamesWithTenantNamesAsync();

        //VERIFY
        keyPairs.ShouldEqual(new List<(string databaseName, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Default Database", false, addTenantDefaultDatabase ? new List<string>{ "Tenant1"} : new List<string>()),
            ("Another", false, new List<string>{ "Tenant2"}),
            ("Bad: No DatabaseName", null, new List<string>()),
            ("Special Postgres", null, new List<string>())
        });
    }
}