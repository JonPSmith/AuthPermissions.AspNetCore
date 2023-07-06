// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSharding;

public class TestShardingConnectionString
{
    private readonly ITestOutputHelper _output;
    private readonly IOptionsSnapshot<ConnectionStringsOption> _connectSnapshot;
    private readonly IOptionsMonitor<ShardingSettingsOption> _shardingMonitor;

    public TestShardingConnectionString(ITestOutputHelper output)
    {
        _output = output;

        var config = AppSettings.GetConfiguration("..\\Test\\TestData", "combinedshardingsettings.json");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        services.Configure<ShardingSettingsOption>(config);
        var serviceProvider = services.BuildServiceProvider();

        _connectSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
        _shardingMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ShardingSettingsOption>>();
    }

    private static AuthPermissionsOptions FormAuthOptionsForSharding(
        AuthPDatabaseTypes databaseType = AuthPDatabaseTypes.SqlServer)
    {
        var options = new AuthPermissionsOptions
        {
            SecondPartOfShardingFile = "Test",
            InternalData =
            {
                AuthPDatabaseType = databaseType
            }
        };
        return options;
    }

    [Fact]
    public void TestDefaultShardingDatabaseData()
    {
        //SETUP
        var databaseDefault = new DatabaseInformationOptions();

        //ATTEMPT
        databaseDefault.FormDefaultDatabaseInfo(FormAuthOptionsForSharding());
        var defaults = databaseDefault.ProvideEmptyDefaultDatabaseInformations();

        //VERIFY
        databaseDefault.Name.ShouldEqual("Default Database");
        databaseDefault.DatabaseName.ShouldBeNull();
        databaseDefault.ConnectionName.ShouldEqual("DefaultConnection");
        databaseDefault.DatabaseType.ShouldEqual("SqlServer");
        defaults.Count.ShouldEqual(1);
    }

    [Fact]
    public void TestDefaultShardingDatabaseData_CustomDatabase()
    {
        //SETUP
        var databaseDefault = new DatabaseInformationOptions();
        databaseDefault.DatabaseType = "Sqlite";

        //ATTEMPT
        databaseDefault.FormDefaultDatabaseInfo(FormAuthOptionsForSharding());
        var defaults = databaseDefault.ProvideEmptyDefaultDatabaseInformations();

        //VERIFY
        databaseDefault.Name.ShouldEqual("Default Database");
        databaseDefault.DatabaseName.ShouldBeNull();
        databaseDefault.ConnectionName.ShouldEqual("DefaultConnection");
        databaseDefault.DatabaseType.ShouldEqual("Sqlite");
        defaults.Count.ShouldEqual(1);
    }


    [Fact]
    public void TestDefaultShardingDatabaseData_CustomDatabase_NoContext()
    {
        //SETUP
        var databaseDefault = new DatabaseInformationOptions();

        //ATTEMPT
        try
        {
            databaseDefault.FormDefaultDatabaseInfo(FormAuthOptionsForSharding(AuthPDatabaseTypes.CustomDatabase));
        }
        catch (AuthPermissionsException e)
        {
            e.Message.ShouldEqual("You are using custom database, so you set the DatabaseType to the short form of the database provider name, e.g. SqlServer.");
            return;
        }

        //VERIFY
        true.ShouldBeFalse();
    }

    [Fact]
    public void TestDefaultShardingDatabaseData_Empty()
    {
        //SETUP

        //ATTEMPT
        var databaseDefault = new DatabaseInformationOptions(false);
        var defaults = databaseDefault.ProvideEmptyDefaultDatabaseInformations();

        //VERIFY
        databaseDefault.AddIfEmpty.ShouldBeFalse();
        defaults.Count.ShouldEqual(0);
    }

    [Fact]
    public void TestGetAllConnectionStrings()
    {
        //SETUP
        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            null, FormAuthOptionsForSharding(), new DatabaseInformationOptions(), ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

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
        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            null, FormAuthOptionsForSharding(), new DatabaseInformationOptions(), ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

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
        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            null, FormAuthOptionsForSharding(), new DatabaseInformationOptions(), ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var connectionString = service.FormConnectionString("Another");

        //VERIFY
        connectionString.ShouldEqual("Data Source=MyServer;Initial Catalog=AnotherDatabase");
    }

    [Fact]
    public void TestGetNamedConnectionStringSqlServer_NoDatabaseName()
    {
        //SETUP
        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            null, FormAuthOptionsForSharding(), new DatabaseInformationOptions(), ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var ex = Assert.Throws<AuthPermissionsException>(() => service.FormConnectionString("Bad: No DatabaseName"));

        //VERIFY
        ex.Message.ShouldEqual("The DatabaseName can't be null or empty when the connection string doesn't have a database defined.");
    }

    [Fact]
    public void TestGetNamedConnectionStringDefaultDatabase()
    {
        //SETUP
        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            null, FormAuthOptionsForSharding(), new DatabaseInformationOptions(), ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var connectionString = service.FormConnectionString("Default Database");

        //VERIFY
        connectionString.ShouldEqual("Server=(localdb)\\mssqllocaldb;Database=AuthPermissions-Test;Trusted_Connection=True;MultipleActiveResultSets=true");
    }

    [Fact]
    public void TestGetNamedConnectionStringPostgres()
    {
        //SETUP
        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            null, FormAuthOptionsForSharding(AuthPDatabaseTypes.PostgreSQL),
            new DatabaseInformationOptions(), 
            ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

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

        var tenant1 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
        tenant1.UpdateShardingState("Default Database", false);
        var tenant2 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant3");
        tenant2.UpdateShardingState("Default Database", false);
        var tenant3 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant2");
        tenant3.UpdateShardingState("Another", false);
        context.AddRange(tenant1, tenant2, tenant3);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));

        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            context, FormAuthOptionsForSharding(),
            new DatabaseInformationOptions(), ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

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
            var tenant1 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
            tenant1.UpdateShardingState(FormAuthOptionsForSharding().ShardingDefaultDatabaseInfoName, true);
            context.Add(tenant1);
        }
        var tenant2 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant2");
        tenant2.UpdateShardingState("Another", false);
        context.Add(tenant2);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var config = AppSettings.GetConfiguration("..\\Test\\TestData");
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));

        var service = new ShardingConnectionsJsonFile(_connectSnapshot, _shardingMonitor,
            context, FormAuthOptionsForSharding(), new DatabaseInformationOptions(), 
            ShardingHelpers.GetDatabaseSpecificMethods(),
            "en".SetupAuthPLoggingLocalizer());

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