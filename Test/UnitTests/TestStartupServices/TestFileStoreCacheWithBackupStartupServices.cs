// Copyright (c) 2024 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.


using System.Diagnostics;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;
using Test.StubClasses;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using AuthPermissions.BaseCode;
using Microsoft.Extensions.Logging;
using Xunit.Extensions.AssertExtensions;
using StackExchange.Redis;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.Graph;
using org.apache.zookeeper.data;

namespace Test.UnitTests.TestStartupServices;

public class TestFileStoreCacheWithBackupStartupServices
{

    private readonly ITestOutputHelper _output;

    public TestFileStoreCacheWithBackupStartupServices(ITestOutputHelper output)
    {
        _output = output;
    }

    public List<LogOutput> Logs { get; set; } = new List<LogOutput>();

    [Fact]
    public void Test_Equals_ShardingEntry()
    {
        //SETUP
        var a = new ShardingEntry
        {
            Name = "Other Database",
            DatabaseName = "MyDatabase1",
            ConnectionName = "AnotherConnectionString",
            DatabaseType = nameof(AuthPDatabaseTypes.SqlServer)
        };
        var b = new ShardingEntry
        {
            Name = "PostgreSql1",
            ConnectionName = "PostgreSqlConnection",
            DatabaseName = "StubTest",
            DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL)
        };

        //ATTEMPT

        //VERIFY
        (a.Equals(b)).ShouldBeFalse();
        (a.Equals(a)).ShouldBeTrue();
    }

    [Fact]
    public async void TestShardingBackup_1_NoEntries()
    {
        //SETUP
        var serviceProvider = SetupServiceToTest(true);
        var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
        fsCache.ClearAll();
        authDbContext.Database.EnsureClean();
        authDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var startupService = new StartupServiceShardingBackup();
        await startupService.ApplyYourChangeAsync(serviceProvider);

        //VERIFY
        foreach (var log in Logs)
        {
            _output.WriteLine(log.ToString());
        }
        Logs.Single().Message.ShouldEqual("No ShardingEntries in both FileStore and ShardingEntryBackup, " +
                                          "which means there isn't anything to do. " +
                                          "This case occurs when the app hasn't any tenants.");
    }

    [Fact]
    public async void TestShardingBackup_2_EmptyBackupShardings()
    {
        //SETUP
        var serviceProvider = SetupServiceToTest(true);
        var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
        authDbContext.Database.EnsureClean();
        authDbContext.ChangeTracker.Clear();

        //ATTEMPT
        var startupService = new StartupServiceShardingBackup();
        await startupService.ApplyYourChangeAsync(serviceProvider);

        //VERIFY
        foreach (var log in Logs)
        {
            _output.WriteLine(log.ToString());
        }

        var fsCacheShardings = fsCache.GetAllKeyValues()
            .Select(s => fsCache.GetClassFromString<ShardingEntry>(s.Value)).ToList();
        var backupSharings = authDbContext.ShardingEntryBackup.ToList();
        fsCacheShardings.Count.ShouldEqual(3);
        backupSharings.Count.ShouldEqual(3);
        foreach (var fsCacheSharding in fsCacheShardings)
        {
            fsCacheSharding.Equals(backupSharings.SingleOrDefault(x => x.Name == fsCacheSharding.Name));
        }
        Logs.Single().Message.ShouldEqual("The FileStore Cache's ShardingEntries has entries, " +
                                          "but the ShardingBackup database is empty. This means that the FileStore Cache's " +
                                          "ShardingEntries entries are copied into the ShardingBackup database.");
    }

    [Fact]
    public async void TestShardingBackup_3_CheckOk()
    {
        //SETUP
        var serviceProvider = SetupServiceToTest(true);
        var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

        //ATTEMPT
        var startupService = new StartupServiceShardingBackup();
        await startupService.ApplyYourChangeAsync(serviceProvider);

        //VERIFY
        foreach (var log in Logs)
        {
            _output.WriteLine(log.ToString());
        }

        var fsCacheShardings = fsCache.GetAllKeyValues()
            .Select(s => fsCache.GetClassFromString<ShardingEntry>(s.Value)).ToList();
        var backupSharings = authDbContext.ShardingEntryBackup.ToList();
        fsCacheShardings.Count.ShouldEqual(3);
        backupSharings.Count.ShouldEqual(3);
        foreach (var fsCacheSharding in fsCacheShardings)
        {
            fsCacheSharding.Equals(backupSharings.SingleOrDefault(x => x.Name == fsCacheSharding.Name));
        }
        Logs.Single().Message.ShouldEqual(
            "Everything is correct: There are 3 ShardingEntries in the " +
            "FileStore Cache which matches the ShardingEntries in the sharding backup database.");
    }

    [Fact]
    public async void TestShardingBackup_3_MissingFsCache()
    {
        //SETUP
        var serviceProvider = SetupServiceToTest(true);
        var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

        //Remove a fsCache entry
        fsCache.Remove(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey("Other Database"));

        //ATTEMPT
        try
        {
            var startupService = new StartupServiceShardingBackup();
            await startupService.ApplyYourChangeAsync(serviceProvider);
        }
        catch (Exception e)
        {
            foreach (var log in Logs)
            {
                _output.WriteLine(log.ToString());
            }

            _output.WriteLine(e.Message);
            e.Message.ShouldEqual(
                "The ShardingEntry entries are either have " +
                "missing entries and / or some of the ShardingEntry don't match the with backup versions. " +
                "See the 'Backup your shardings' section in the AuthP's Wiki for what to do if this happens.");
            return;
        }

        //VERIFY
        false.ShouldBeTrue();
    }

    [Fact]
    public async void TestShardingBackup_3_MissingBackupShadings()
    {
        //SETUP
        var serviceProvider = SetupServiceToTest(true);
        var fsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

        //Remove a ShardingEntryBackup entry
        var toDelete = authDbContext.ShardingEntryBackup.Single(x => x.Name == "Other Database");
        authDbContext.ShardingEntryBackup.Remove(toDelete);
        authDbContext.SaveChanges();

        //ATTEMPT
        try
        {
            var startupService = new StartupServiceShardingBackup();
            await startupService.ApplyYourChangeAsync(serviceProvider);
        }
        catch (Exception e)
        {
            foreach (var log in Logs)
            {
                _output.WriteLine(log.ToString());
            }

            _output.WriteLine(e.Message);
            e.Message.ShouldEqual(
                "The ShardingEntry entries are either have " +
                "missing entries and / or some of the ShardingEntry don't match the with backup versions. " +
                "See the 'Backup your shardings' section in the AuthP's Wiki for what to do if this happens.");
            return;
        }

        //VERIFY
        false.ShouldBeTrue();
    }


    //----------------------------------------------------------------------
    //support methods 

    private ServiceProvider SetupServiceToTest(bool hybridMode)
    {
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
        StubFsCache = new StubFileStoreCacheClass(); //creates a new instance, which will be empty (because we are using the StubFileStoreCacheClass)

        testEntries.ForEach(x => StubFsCache.SetClass(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(x.Name), x));
        AuthDbContext.ShardingEntryBackup.AddRange(testEntries);
        AuthDbContext.SaveChanges();

        var services = new ServiceCollection();
        services.AddSingleton(StubFsCache);
        services.AddSingleton(AuthDbContext);
        services.AddSingleton(new AuthPermissionsOptions());
        services.AddSingleton(new ShardingEntryOptions(hybridMode));
        services.AddSingleton(x =>
            new LoggerFactory(
                    new[] { new MyLoggerProviderActionOut(l => Logs.Add(l)) })
                .CreateLogger<StartupServiceShardingBackup>());
        var serviceProvider = services.BuildServiceProvider();
        Logs = new List<LogOutput>(); //Remove previous logs

        return serviceProvider;
    }

     private AuthPermissionsDbContext AuthDbContext { get; set; }
     private IDistributedFileStoreCacheClass StubFsCache { get; set; }
}