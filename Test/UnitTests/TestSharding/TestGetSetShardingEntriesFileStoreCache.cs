// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;
using AuthPermissions.BaseCode.DataLayer.Classes;
using Microsoft.Extensions.Options;
using Net.DistributedFileStoreCache;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSharding;

public class TestGetSetShardingEntriesFileStoreCache
{
    private readonly ITestOutputHelper _output;
    public TestGetSetShardingEntriesFileStoreCache(ITestOutputHelper output)
    {
        _output = output;
    }

    private static AuthPermissionsOptions FormAuthOptionsForSharding(
        AuthPDatabaseTypes databaseType = AuthPDatabaseTypes.SqlServer)
    {
        var options = new AuthPermissionsOptions
        {
            DefaultShardingEntryName = "Default Database",
            InternalData =
            {
                AuthPDatabaseType = databaseType
            }
        };
        return options;
    }

    private static string FormShardingEntryKey(string shardingEntryName) => "ShardingEntry-" + shardingEntryName;

    private static IDistributedFileStoreCacheClass CreateFileStoreCacheWithData()
    {
        var testEntries = new List<ShardingEntry>
        {
            new (){ Name = "Default Database", ConnectionName = "UnitTestConnection", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer)},
            new (){ Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "AnotherConnectionString", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer) },
            new (){ Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL) }
        };
        var stubFsCache = new StubFileStoreCacheClass();
        testEntries.ForEach(x => stubFsCache.SetClass(FormShardingEntryKey(x.Name), x));

        return stubFsCache;
    }

    private class SetupServiceToTest
    {
        public AuthPermissionsDbContext AuthDbContext { get; }
        public IDistributedFileStoreCacheClass StubFsCache { get; }
        public IGetSetShardingEntries Service { get; }

        public SetupServiceToTest(bool tenantsInAuthPdb = true, AuthPDatabaseTypes databaseType = AuthPDatabaseTypes.SqlServer)
        {
            var config = AppSettings.GetConfiguration("..\\Test\\TestData", "combinedshardingsettings.json");
            var services = new ServiceCollection();
            services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
            var serviceProvider = services.BuildServiceProvider();
            var connectSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();

            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
            AuthDbContext = new AuthPermissionsDbContext(options);
            AuthDbContext.Database.EnsureClean();
            StubFsCache = CreateFileStoreCacheWithData();
            Service = new GetSetShardingEntriesFileStoreCache(connectSnapshot,
                new ShardingEntryOptions(tenantsInAuthPdb),
                FormAuthOptionsForSharding(databaseType), AuthDbContext,
                StubFsCache, new List<IDatabaseSpecificMethods>{new SqlServerDatabaseSpecificMethods()},
                "en".SetupAuthPLoggingLocalizer());
        }
    }

    [Fact]
    public void TestGetAllShardingEntries()
    {
        //SETUP
        var setup = new SetupServiceToTest();

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();

        //VERIFY
        foreach (var databaseInformation in shardings)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        shardings.Count.ShouldEqual(3);
        shardings[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
        shardings[1].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
        shardings[2].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: PostgreSQL");
    }

    [Fact]
    public void TestGetAllShardingEntries_NoFile_AddIfEmptyTrue()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        setup.StubFsCache.ClearAll();

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();

        //VERIFY
        foreach (var databaseInformation in shardings)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        shardings.Single().ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    [Fact]
    public void TestGetAllShardingEntries_NoFile_AddIfEmptyFalse()
    {
        //SETUP
        var setup = new SetupServiceToTest(false);
        setup.StubFsCache.ClearAll();

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();

        //VERIFY
        shardings.Count.ShouldEqual(0);
        setup.StubFsCache.GetAllKeyValues().Count().ShouldEqual(0);
    }

    [Fact]
    public void TestGetAllShardingEntries_NoFile_CustomDatabase()
    {
        //SETUP
        var setup = new SetupServiceToTest(true, AuthPDatabaseTypes.CustomDatabase);
        setup.StubFsCache.ClearAll();

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();

        //VERIFY
        shardings[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    [Theory]
    [InlineData("Other Database", true)]
    [InlineData("Not Found", false)]
    public void TestGetSingleShardingEntry(string shardingName, bool foundOk)
    {
        //SETUP
        var setup = new SetupServiceToTest();

        //ATTEMPT
        var sharding = setup.Service.GetSingleShardingEntry(shardingName);

        //VERIFY
        _output.WriteLine(sharding?.ToString() ?? "- no entry -");
        if (foundOk)
            sharding.ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
        else
            sharding.ShouldBeNull();
    }

    [Theory]
    [InlineData(true,"Default Database", true)]
    [InlineData(false, "Default Database", false)]
    [InlineData(true, "Not Found", false)]
    [InlineData(false, "Not Found", false)]
    public void TestGetSingleShardingEntry_NoFile_AddIfEmpty(bool addIfEmpty, string shardingName, bool foundOk)
    {
        //SETUP
        var setup = new SetupServiceToTest(addIfEmpty);
        setup.StubFsCache.ClearAll();

        //ATTEMPT
        var sharding = setup.Service.GetSingleShardingEntry(shardingName);

        //VERIFY
        _output.WriteLine(sharding?.ToString() ?? "- no entry -");
        if (foundOk)
            sharding.ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        else
            sharding.ShouldBeNull();
    }

    [Theory]
    [InlineData("New Entry", false)]
    [InlineData("Other Database", true)]
    public void TestAddNewShardingEntry_Duplicate(string shardingName, bool duplicate)
    {
        //SETUP
        var setup = new SetupServiceToTest();
        var entry = new ShardingEntry
        {
            Name = shardingName,
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };

        //ATTEMPT
        var status = setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(duplicate);
        setup.StubFsCache.GetAllKeyValues().Count().ShouldEqual( duplicate ? 3 : 4);
    }

    [Theory]
    [InlineData("SqlServer", "DefaultConnection", false)]
    [InlineData("SqlServer", "PostgresConnection",true)]
    public void TestAddNewShardingEntry_CorrectDatabaseProvider(string databaseType, string connectionName,  bool fail)
    {
        //SETUP
        var setup = new SetupServiceToTest();
        var entry = new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = connectionName,
            DatabaseName = "My database",
            DatabaseType = databaseType
        };

        //ATTEMPT
        var status = setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(fail);
    }

    [Fact]
    public void TestAddNewShardingEntry_DefaultDatabaseBad()
    {
        //SETUP
        var setup = new SetupServiceToTest();
        var entry = new ShardingEntry
        {
            Name = "Default Database",
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };

        //ATTEMPT
        var status = setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.GetAllErrors().ShouldEqual("You can't add, update or delete the default sharding entry called 'Default Database'.");
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void TestAddNewShardingEntry_NoFile(bool addIfEmpty, int numEntries)
    {
        //SETUP
        var setup = new SetupServiceToTest(addIfEmpty);
        setup.StubFsCache.ClearAll();

        var entry = new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = "AnotherConnectionString",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };

        //ATTEMPT
        var status = setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        setup.StubFsCache.GetAllKeyValues().Count().ShouldEqual(numEntries);
    }

    [Theory]
    [InlineData("Other Database", false)]
    [InlineData("New Entry", true)]
    public void UpdateShardingEntry(string shardingName, bool fail)
    {
        //SETUP
        var setup = new SetupServiceToTest();
        var entry = new ShardingEntry
        {
            Name = shardingName,
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };

        //ATTEMPT
        var status = setup.Service.UpdateShardingEntry(entry);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(fail);
        if (!fail)
        {
            var updated = setup.StubFsCache.GetClass<ShardingEntry>(FormShardingEntryKey(shardingName));
            updated.ToString().ShouldEqual("Name: Other Database, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        }
    }

    [Theory]
    [InlineData("Other Database", false)]
    [InlineData("Not in cache", true)]
    public void RemoveShardingEntry(string shardingName, bool fail)
    {
        //SETUP
        var setup = new SetupServiceToTest();

        //ATTEMPT
        var status = setup.Service.RemoveShardingEntry(shardingName);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(fail);
        setup.StubFsCache.GetAllKeyValues().Count.ShouldEqual(fail ? 3 : 2);
    }

    [Fact]
    public void RemoveShardingEntry_FailDueToUsers()
    {
        //SETUP
        var setup = new SetupServiceToTest();
        {
            //This creates a tenant and a user that uses the "Other Database"
            var tenantStatus = Tenant.CreateSingleTenant("Tenant1",
                "en".SetupAuthPLoggingLocalizer().DefaultLocalizer);
            tenantStatus.Result.UpdateShardingState("Other Database", false);
            var userStatus = AuthUser.CreateAuthUser("123", "Me@g1.com", null,
                new List<RoleToPermissions>(), "en".SetupAuthPLoggingLocalizer().DefaultLocalizer, tenantStatus.Result);
            setup.AuthDbContext.Add(userStatus.Result);
            setup.AuthDbContext.SaveChanges();
        }

        //ATTEMPT
        var status = setup.Service.RemoveShardingEntry("Other Database");

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldBeTrue();
        status.GetAllErrors().ShouldEqual("You need to disconnect the 1 users from the 'Other Database' sharding entry before you can delete it.");
    }

    [Fact]
    public void TestFormingConnectionString_MissingDatabaseSpecificMethods()
    {
        //SETUP
        var setup = new SetupServiceToTest();
        var entry = new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = "PostgresConnection",
            DatabaseName = "My database",
            DatabaseType = "PostgreSQL"
        };

        //ATTEMPT
        try
        {
            var status = setup.Service.AddNewShardingEntry(entry);
            _output.WriteLine(status.GetAllErrors());
        }
        catch (Exception e)
        {
            e.Message.ShouldEqual("The PostgreSQL database provider isn't supported. You need to register a IDatabaseSpecificMethods method for that database type, e.g. SqlServerDatabaseSpecificMethods.");
            return;
        }

        //VERIFY
        false.ShouldBeTrue();
    }

    [Fact]
    public void TestGetAllConnectionStrings_Hybrid()
    {
        //SETUP
        var setup = new SetupServiceToTest();

        //ATTEMPT
        var connectionNames = setup.Service.GetConnectionStringNames().ToList();

        //VERIFY
        foreach (var name in connectionNames)
        {
            _output.WriteLine(name);
        }
        connectionNames.Count.ShouldEqual(4);
        connectionNames[0].ShouldEqual("AnotherConnectionString");
        connectionNames[1].ShouldEqual("DefaultConnection");
        connectionNames[2].ShouldEqual("PostgresConnection");
        connectionNames[3].ShouldEqual("ServerOnlyConnectionString");
    }

    [Fact]
    public void TestGetAllConnectionStrings_ShardOnly()
    {
        //SETUP
        var setup = new SetupServiceToTest(false);

        //ATTEMPT
        var connectionNames = setup.Service.GetConnectionStringNames().ToList();

        //VERIFY
        foreach (var name in connectionNames)
        {
            _output.WriteLine(name);
        }
        connectionNames.Count.ShouldEqual(3);
        connectionNames[0].ShouldEqual("AnotherConnectionString");
        connectionNames[1].ShouldEqual("PostgresConnection");
        connectionNames[2].ShouldEqual("ServerOnlyConnectionString");
    }

    [Fact]
    public void TestFormConnectionString()
    {
        //SETUP
        var setup = new SetupServiceToTest();

        //ATTEMPT
        var connectionString = setup.Service.FormConnectionString("Other Database");

        //VERIFY
        connectionString.ShouldEqual("Data Source=MyServer;Initial Catalog=MyDatabase1");
    }

    [Fact]
    public void TestFormConnectionString_Bad()
    {
        //SETUP
        var setup = new SetupServiceToTest();

        //ATTEMPT
        try
        {
            var connectionString = setup.Service.FormConnectionString("PostgreSql1");
        }
        catch (Exception e)
        {
            e.Message.ShouldEqual("Could not find the connection name 'PostgreSqlConnection' that the sharding database data 'PostgreSql1' requires.");
            return;
        }

        //VERIFY
        false.ShouldBeTrue();
    }

    [Fact]
    public async Task TestQueryTenantsSingle()
    {
        //SETUP
        var setup = new SetupServiceToTest();

        var tenant1 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
        tenant1.UpdateShardingState("Default Database", false);
        var tenant2 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant3");
        tenant2.UpdateShardingState("Default Database", false);
        var tenant3 = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant2");
        tenant3.UpdateShardingState("Other Database", true);
        setup.AuthDbContext.AddRange(tenant1, tenant2, tenant3);
        setup.AuthDbContext.SaveChanges();

        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var list = await setup.Service.GetShardingsWithTenantNamesAsync();

        //VERIFY
        list.ShouldEqual(new List<(string databaseName, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Default Database", false, new List<string>{"Tenant1", "Tenant3"}),
            ("Other Database", true, new List<string>{ "Tenant2"}),
            ("PostgreSql1", null, new List<string>())
        });
    }

}