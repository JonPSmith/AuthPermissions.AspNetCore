// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.DependencyInjection;
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

    //NOTE: if HybridMode is true, then the FileStore Cache has the "DefaultEntry",
    //but the ShardingEntryBackup doesn’t have the "DefaultEntry"
    //The SetupServiceToTest class's ShardingEntryBackup returns the correct entries
    //to match the HybridMode, i.e. it doesn't have the "DefaultEntry" when the HybridMode is true
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestCheckSetupIsCorrect(bool hybridMode)
    {
        //SETUP
        var setup = new SetupServiceToTest(hybridMode);

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();
        var shardingBackup = setup.AuthDbContext.ShardingEntryBackup.ToArray();

        //VERIFY
        foreach (var databaseInformation in shardings)
        {
            _output.WriteLine(databaseInformation.ToString());
        }

        if (hybridMode)
        {
            shardings.Count.ShouldEqual(3);
            shardings[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
            shardings[1].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
            shardings[2].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: PostgreSQL");
            shardingBackup.Length.ShouldEqual(3);
            shardingBackup[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
            shardingBackup[1].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
            shardingBackup[2].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: PostgreSQL");
        }
        else
        {
            shardings.Count.ShouldEqual(3);
            shardings[0].ToString().ShouldEqual("Name: FirstSharding, DatabaseName: Sharding001Db, ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
            shardings[1].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
            shardings[2].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: PostgreSQL");
            shardingBackup.Length.ShouldEqual(3);
            shardingBackup[0].ToString().ShouldEqual("Name: FirstSharding, DatabaseName: Sharding001Db, ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
            shardingBackup[1].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
            shardingBackup[2].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: PostgreSQL");
        }
    }

    //------------------------------------------------------------------
    //Section to check what happens if the FileStore cache is empty,
    //which covering when a new FileStore cache is created
    //and following the hybrid / Sharding-Only modes

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestFileStoreCacheEmpty_ReadShardings(bool hybridMode)
    {
        //SETUP
        var setup = new SetupServiceToTest(hybridMode);
        setup.StubFsCache.ClearAll();
        setup.AuthDbContext.Database.EnsureClean();

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();

        //VERIFY
        foreach (var databaseInformation in shardings)
        {
            _output.WriteLine(databaseInformation.ToString());
        }

        if (hybridMode)
        {
            shardings.Single().ToString().ShouldEqual(
                "Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        }
        else
        {
            shardings.Count().ShouldEqual(0);
        }
        setup.AuthDbContext.ShardingEntryBackup.Count().ShouldEqual(0);
    }

    [Fact]
    public void TestFileStoreCacheEmpty_CustomDatabase()
    {
        //SETUP
        var setup = new SetupServiceToTest(true, AuthPDatabaseTypes.CustomDatabase);
        setup.StubFsCache.ClearAll();

        //ATTEMPT
        var shardings = setup.Service.GetAllShardingEntries();

        //VERIFY
        shardings[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    //--------------------------------------------------------------
    //Section to look at read, add, update and delete a sharding
    //including checking that the sharding is valid

    [Theory]
    [InlineData("Other Database", true)]
    [InlineData("Not Found", false)]
    public void TestReadSingleShardingEntry(string shardingName, bool foundOk)
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        //ATTEMPT
        var sharding = setup.Service.GetSingleShardingEntry(shardingName);

        //VERIFY
        _output.WriteLine(sharding?.ToString() ?? "- no entry -");
        if (foundOk)
        {
            sharding.ToString().ShouldEqual(
                "Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: AnotherConnectionString, DatabaseType: SqlServer");
        }
        else
            sharding.ShouldBeNull();
    }

    [Fact]
    public void TestAddNewShardingEntry_Good()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        var entry = new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };

        //ATTEMPT
        setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        setup.StubFsCache.GetAllKeyValues().Count().ShouldEqual(4);
        setup.Service.GetSingleShardingEntry(entry.Name).ToString().ShouldEqual(
            "Name: New Entry, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        setup.AuthDbContext.ShardingEntryBackup.Count().ShouldEqual(4);
        setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == entry.Name).ToString().ShouldEqual(
            "Name: New Entry, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    [Fact]
    public void TestAddNewShardingEntry_Empty_Hybrid()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        var entry = new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };
        setup.StubFsCache.ClearAll();
        setup.AuthDbContext.Database.EnsureClean();
        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        var shardings = setup.Service.GetAllShardingEntries();
        var shardingBackup = setup.AuthDbContext.ShardingEntryBackup.ToArray();
        shardings.Count().ShouldEqual(2);
        shardings[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        shardings[1].ToString().ShouldEqual("Name: New Entry, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        shardingBackup.Count().ShouldEqual(2);
        shardingBackup[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        shardingBackup[1].ToString().ShouldEqual("Name: New Entry, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");

    }

    [Theory]
    [InlineData("New Entry", false)]
    [InlineData("Other Database", true)]
    public void TestAddNewShardingEntry_Duplicate(string shardingName, bool duplicate)
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
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
    }

    [Theory]
    [InlineData("SqlServer", "DefaultConnection", false)]
    [InlineData("SqlServer", "PostgresConnection",true)]
    public void TestAddNewShardingEntry_CorrectDatabaseProvider(string databaseType, string connectionName,  bool fail)
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
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
    public void TestAddNewShardingEntry_Hybrid_DefaultDatabaseBad()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
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
    [InlineData("Other Database", false)]
    [InlineData("New Entry", true)]
    public void UpdateShardingEntry(string shardingName, bool fail)
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        var entry = new ShardingEntry
        {
            Name = shardingName,
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };
        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var status = setup.Service.UpdateShardingEntry(entry);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(fail);
        if (!fail)
        {
            setup.StubFsCache.GetClass<ShardingEntry>(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(shardingName)).ToString()
                .ShouldEqual("Name: Other Database, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
            setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == shardingName).ToString()
                .ShouldEqual("Name: Other Database, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        }
    }

    [Theory]
    [InlineData("Other Database", false)]
    [InlineData("Not in cache", true)]
    public void RemoveShardingEntry_HybridMode(string shardingName, bool fail)
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        //ATTEMPT
        var status = setup.Service.RemoveShardingEntry(shardingName);

        //VERIFY
        _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(fail);
        setup.StubFsCache.GetAllKeyValues().Count.ShouldEqual(fail ? 3 : 2);
        setup.AuthDbContext.ShardingEntryBackup.Count().ShouldEqual(fail ? 3 : 2);
    }

    [Fact]
    public void RemoveShardingEntry_FailDueToUsers()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
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

    //--------------------------------------------------------------
    //Section to check that the shardingEntryBackup will provide the right 
    //shardingBackup even if the data in the shardingEntryBackup isn't correct

    /// <summary>
    /// This checks that if there is a ShardingEntryBackup with the same Name as the
    /// new entry, then it will update the existing entry to the correct ShardingEntry data 
    /// </summary>
    [Fact]
    public void TestAddNewShardingEntry_ShardingEntryBackup_Duplicate()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        //put the wrong data into the ShardingEntryBackup db
        setup.AuthDbContext.ShardingEntryBackup.Add(new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = "Bad data",
            DatabaseName = "Bad data",
            DatabaseType = "Bad data"
        });
        setup.AuthDbContext.SaveChanges();
        setup.AuthDbContext.ChangeTracker.Clear();

        //Now back to the normal code
        var entry = new ShardingEntry
        {
            Name = "New Entry",
            ConnectionName = "DefaultConnection",
            DatabaseName = "My database",
            DatabaseType = "SqlServer"
        };

        //ATTEMPT
        var status = setup.Service.AddNewShardingEntry(entry);

        //VERIFY
        status.IsValid.ShouldBeTrue();
        setup.StubFsCache.GetAllKeyValues().Count().ShouldEqual(4);
        setup.Service.GetSingleShardingEntry(entry.Name).ToString().ShouldEqual(
            "Name: New Entry, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        setup.AuthDbContext.ShardingEntryBackup.Count().ShouldEqual(4);
        setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == entry.Name).ToString().ShouldEqual(
            "Name: New Entry, DatabaseName: My database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    [Fact]
    public void UpdateShardingEntry_ShardingEntryBackup_Missing()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        //Delete the ShardingEntryBackup entry that will be updated.
        var toDelete = setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == "Other Database");
        setup.AuthDbContext.Remove(toDelete);
        setup.AuthDbContext.SaveChanges();
        setup.AuthDbContext.ChangeTracker.Clear();

        var entry = new ShardingEntry
        {
            Name = "Other Database",
            ConnectionName = "DefaultConnection",
            DatabaseName = "My different database",
            DatabaseType = "SqlServer"
        };
        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var status = setup.Service.UpdateShardingEntry(entry);

        //VERIFY
        status.IsValid.ShouldBeTrue();
        setup.Service.GetSingleShardingEntry(entry.Name).ToString()
                .ShouldEqual("Name: Other Database, DatabaseName: My different database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
        setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == entry.Name).ToString()
                .ShouldEqual("Name: Other Database, DatabaseName: My different database, ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    [Fact]
    public void RemoveShardingEntry_ShardingEntryBackup_AlreadyDeleted()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        //Delete the ShardingEntryBackup entry
        var toDelete = setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == "Other Database");
        setup.AuthDbContext.Remove(toDelete);
        setup.AuthDbContext.SaveChanges();
        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var status = setup.Service.RemoveShardingEntry("Other Database");

        //VERIFY
        status.IsValid.ShouldBeTrue();
        setup.StubFsCache.GetAllKeyValues().Count.ShouldEqual(2);
        setup.Service.GetSingleShardingEntry("Other Database").ShouldBeNull();
        setup.AuthDbContext.ShardingEntryBackup.Count().ShouldEqual(2);
        setup.AuthDbContext.ShardingEntryBackup.SingleOrDefault(x => x.Name == "Other Database").ShouldBeNull();
    }

    //--------------------------------------------------------------
    //Section about the CheckTwoShardingSources method

    [Theory]
    [InlineData(true, ", apart of the default database.")]
    [InlineData(false, "there are no sharding databases.")]
    public void TestShardingBackup_Part1_NoEntries(bool hybridMode, string messageEnds)
    {
        //SETUP
        var setup = new SetupServiceToTest(hybridMode);

        setup.StubFsCache.ClearAll();
        setup.AuthDbContext.Database.EnsureClean();
        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeTrue();
        var logs = setup.StubLocalizer.Logs;
        logs.Single().ActualMessage.ShouldEndWith(messageEnds);
    }

    [Fact]
    public void TestShardingBackup_Part2_BackupShardingFilled()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        setup.AuthDbContext.Database.EnsureClean();
        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeTrue();
        var logs = setup.StubLocalizer.Logs;
        logs.Single().ActualMessage.ShouldEqual(
            "BACKUP-SHARDINGS: The shardings backup database was empty, so we copied the 3 sharding entries into the FileStore Cache " +
            "shardings backup database. If the FileStore Cache file is deleted then run the Check again and it " +
            "will update the FileStore Cache.");
    }

    [Fact]
    public void TestShardingBackup_Part3_FillFileStoreCache()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        setup.StubFsCache.ClearAll();

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeTrue();
        var logs = setup.StubLocalizer.Logs;
        logs.Single().ActualMessage.ShouldEqual(
            "RESTORE-SHARDINGS: The FileStore Cache was empty, but the shardings backup database has 3 entries. " +
            "This happens when your FileStore Cache was accidentally deleted, so the Check command " +
            "has copied the missing sharding entries from shardings backup database into the FileStore Cache.");
    }

    [Fact]
    public void TestShardingBackup_Part4_AllGood()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeTrue();
        var logs = setup.StubLocalizer.Logs;
        logs.Single().ActualMessage.ShouldEqual(
            "CHECK-SHARDINGS: All OK. The FileStore Cache shardings entries match the ShardingBackup database entries.");
    }

    [Fact]
    public void TestShardingBackup_Part4_1_MissingFsCache()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        setup.StubFsCache.Remove(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey("Other Database"));

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeFalse();
        var logs = setup.StubLocalizer.Logs;
        logs.Count.ShouldEqual(2);
        logs[0].ActualMessage.ShouldEqual(
            "The FileStore Cache is missing an entry with the Name of 'Other Database'.");
    }

    [Fact]
    public void TestShardingBackup_Part4_1_MissingBackupDb()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        //Remove a ShardingEntryBackup entry
        var toDelete = setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == "Other Database");
        setup.AuthDbContext.ShardingEntryBackup.Remove(toDelete);
        setup.AuthDbContext.SaveChanges();

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeFalse();
        var logs = setup.StubLocalizer.Logs;
        logs.Count.ShouldEqual(2);
        logs[0].ActualMessage.ShouldEqual(
            "The ShardingBackup database is missing an entry with the Name of 'Other Database'.");
    }

    [Fact]
    public void TestShardingBackup_Part4_1_MissingBoth()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        //remove a sharding from FileStore Cache
        setup.StubFsCache.Remove(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey("Other Database"));
        //Remove a ShardingEntryBackup entry
        var toDelete = setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == "PostgreSql1");
        setup.AuthDbContext.ShardingEntryBackup.Remove(toDelete);
        setup.AuthDbContext.SaveChanges();

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeFalse();
        var logs = setup.StubLocalizer.Logs;
        logs.Count.ShouldEqual(3);
        logs[0].ActualMessage.ShouldEqual(
            "The ShardingBackup database is missing an entry with the Name of 'PostgreSql1'.");
        logs[1].ActualMessage.ShouldEqual(
            "The FileStore Cache is missing an entry with the Name of 'Other Database'.");
        logs[2].ActualMessage.ShouldEqual(
            "There are 2 differences which you need to fix manually. Look at the section called " +
            "'Securing the sharding data' in the AuthP's Wiki for more information about that.");
    }

    [Fact]
    public void TestShardingBackup_Part4_2_MatchError_FsCache()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        var shardingName = "Other Database";
        var fsCacheEntryToChange =
            setup.StubFsCache.GetClass<ShardingEntry>(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(shardingName));
        fsCacheEntryToChange.DatabaseName = "DIFFERENT-DB";
        setup.StubFsCache.SetClass(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(shardingName), fsCacheEntryToChange);
        var backupSharding = setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == shardingName);

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeFalse();
        var logs = setup.StubLocalizer.Logs;
        logs.Count.ShouldEqual(4);
        logs[0].ActualMessage.ShouldEqual("The two Shardings with the Name of 'Other Database' do not match. "+
                            "See the two sources that don't match below:");
        logs[1].ActualMessage.ShouldEqual($"FileStore Cache Entry = {fsCacheEntryToChange.ToString()}");
        logs[2].ActualMessage.ShouldEqual($"ShardingBackup Entry = {backupSharding.ToString()}");
        logs[3].ActualMessage.ShouldEqual("There are 1 difference which you need to fix manually. " +
                                          "Look at the section called 'Securing the sharding data' in the AuthP's Wiki for more information about that.");
    }

    [Fact]
    public void TestShardingBackup_Part4_2_MatchError_ShardingBackup()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
        var shardingName = "Other Database";
        var backupShardingToChange = setup.AuthDbContext.ShardingEntryBackup.Single(x => x.Name == shardingName);
        backupShardingToChange.DatabaseName = "DIFFERENT-DB";
        setup.AuthDbContext.ShardingEntryBackup.Update(backupShardingToChange);
        setup.AuthDbContext.SaveChanges();
        var fsCacheEntry =
            setup.StubFsCache.GetClass<ShardingEntry>(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(shardingName));

        setup.AuthDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var status = setup.Service.CheckTwoShardingSources();

        //VERIFY
        status.IsValid.ShouldBeFalse();
        var logs = setup.StubLocalizer.Logs;
        logs.Count.ShouldEqual(4);
        logs[0].ActualMessage.ShouldEqual("The two Shardings with the Name of 'Other Database' do not match. " +
                                          "See the two sources that don't match below:");
        logs[1].ActualMessage.ShouldEqual($"FileStore Cache Entry = {fsCacheEntry.ToString()}");
        logs[2].ActualMessage.ShouldEqual($"ShardingBackup Entry = {backupShardingToChange.ToString()}");
        logs[3].ActualMessage.ShouldEqual("There are 1 difference which you need to fix manually. " +
                                          "Look at the section called 'Securing the sharding data' in the AuthP's Wiki for more information about that.");
    }

    //--------------------------------------------------------------
    //Section to form connection strings and GetShardingsWithTenantNamesAsync

    [Fact]
    public void TestFormingConnectionString_MissingDatabaseSpecificMethods()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);
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
        var setup = new SetupServiceToTest(true);

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
        var setup = new SetupServiceToTest(true);

        //ATTEMPT
        var connectionString = setup.Service.FormConnectionString("Other Database");

        //VERIFY
        connectionString.ShouldEqual("Data Source=MyServer;Initial Catalog=MyDatabase1");
    }

    [Fact]
    public void TestFormConnectionString_Bad()
    {
        //SETUP
        var setup = new SetupServiceToTest(true);

        //ATTEMPT
        try
        {
            setup.Service.FormConnectionString("PostgreSql1");
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
        var setup = new SetupServiceToTest(true);

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
        _output.WriteLine(list[0].ToString());

        //VERIFY
        list.Count.ShouldEqual(3);
        list[0].shardingName.ShouldEqual("Default Database");
        list[0].hasOwnDb.ShouldEqual(false);
        list[0].tenantNames.ShouldEqual(new List<string> { "Tenant1", "Tenant3" });
        list[1].shardingName.ShouldEqual("Other Database");
        list[1].hasOwnDb.ShouldEqual(true);
        list[1].tenantNames.ShouldEqual(new List<string> { "Tenant2" });
        list[2].shardingName.ShouldEqual("PostgreSql1");
        list[2].hasOwnDb.ShouldEqual(null);
        list[2].tenantNames.ShouldEqual(new List<string>( ));
    }

    //------------------------------------------------------------------------
    //Support code to test the 

    private class SetupServiceToTest
    {
        public SetupServiceToTest(bool hybridMode,
            AuthPDatabaseTypes databaseType = AuthPDatabaseTypes.SqlServer)
        {
            var config = AppSettings.GetConfiguration("..\\Test\\TestData", "shardingConnectionStrings.json");
            var services = new ServiceCollection();
            services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
            var serviceProvider = services.BuildServiceProvider();
            var connectSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();
            StubLocalizer = new StubDefaultLocalizerWithLogging("en", GetType());

            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
            AuthDbContext = new AuthPermissionsDbContext(options);
            AuthDbContext.Database.EnsureClean();

            //Now we add the test ShardingEntries and the ShardingEntryBackup database
            var testEntries = new List<ShardingEntry>
            {
                new (){ Name = (hybridMode ? "Default Database" : "FirstSharding"),
                    DatabaseName = (hybridMode ? null : "Sharding001Db"),
                    ConnectionName = "UnitTestConnection", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer)},
                new (){ Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "AnotherConnectionString", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer) },
                new (){ Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL) }
            };
            StubFsCache = new StubFileStoreCacheClass();
            StubFsCache.ClearAll();
            testEntries.ForEach(x => StubFsCache.SetClass(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(x.Name), x));
            AuthDbContext.ShardingEntryBackup.AddRange(testEntries);
            AuthDbContext.SaveChanges();

            Service = new GetSetShardingEntriesFileStoreCache(connectSnapshot,
                new ShardingEntryOptions(hybridMode),
                FormAuthOptionsForSharding(databaseType), AuthDbContext,
                StubFsCache, new List<IDatabaseSpecificMethods>{new SqlServerDatabaseSpecificMethods()},
                new TestAuthPDefaultLocalizer(StubLocalizer));
        }

        public AuthPermissionsDbContext AuthDbContext { get; }
        public IDistributedFileStoreCacheClass StubFsCache { get; }
        public IGetSetShardingEntries Service { get; }
        public StubDefaultLocalizerWithLogging StubLocalizer { get; }
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
}