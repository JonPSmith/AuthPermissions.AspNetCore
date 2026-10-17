// Copyright (c) 2025 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestEfCoreCodePostgres;

/// <summary>
/// RemoveShardingEntry deletes the entry's ShardingEntryBackup row. It used to do that with raw SQL that named the table
/// without quotes, which Postgres folds to lower case and then cannot find, so the call threw after the entry had already
/// been removed from the FileStore Cache and left the backup row behind.
/// </summary>
public class TestRemoveShardingEntry
{
    private static (AuthPermissionsDbContext context, StubFileStoreCacheClass fsCache, IGetSetShardingEntries service) Setup()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSqlConnection"] = "Host=127.0.0.1;Database=ShardingTest;Username=postgres;Password=LetMeIn"
        }).Build();
        var services = new ServiceCollection();
        services.Configure<ConnectionStringsOption>(config.GetSection("ConnectionStrings"));
        var connectSnapshot = services.BuildServiceProvider().GetRequiredService<IOptionsSnapshot<ConnectionStringsOption>>();

        var options = new TestClassHolder().CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var entries = new List<ShardingEntry>
        {
            new() { Name = "Default Database", ConnectionName = "PostgreSqlConnection", DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL) },
            new() { Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "PostgreSqlConnection", DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL) },
        };
        var fsCache = new StubFileStoreCacheClass();
        fsCache.ClearAll();
        entries.ForEach(x => fsCache.SetClass(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(x.Name), x));
        context.ShardingEntryBackup.AddRange(entries);
        context.SaveChanges();

        var authOptions = new AuthPermissionsOptions
        {
            DefaultShardingEntryName = "Default Database",
            InternalData = { AuthPDatabaseType = AuthPDatabaseTypes.PostgreSQL }
        };
        var service = new GetSetShardingEntriesFileStoreCache(connectSnapshot, new ShardingEntryOptions(true), authOptions, context,
            fsCache, new List<IDatabaseSpecificMethods> { new PostgresDatabaseSpecificMethods() },
            new TestAuthPDefaultLocalizer(new StubDefaultLocalizerWithLogging("en", typeof(TestRemoveShardingEntry))));
        return (context, fsCache, service);
    }

    [Fact]
    public void RemoveShardingEntry_RemovesTheBackupRowOnPostgres()
    {
        //SETUP
        var (context, fsCache, service) = Setup();

        //ATTEMPT
        var status = service.RemoveShardingEntry("Other Database");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        fsCache.GetAllKeyValues().Count.ShouldEqual(1);
        context.ShardingEntryBackup.AsEnumerable().Select(x => x.Name).ShouldEqual(new[] { "Default Database" });
    }

    [Fact]
    public void RemoveShardingEntry_BackupRowAlreadyMissing_StillSucceedsOnPostgres()
    {
        //SETUP
        var (context, fsCache, service) = Setup();
        context.ShardingEntryBackup.Remove(context.ShardingEntryBackup.Single(x => x.Name == "Other Database"));
        context.SaveChanges();
        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = service.RemoveShardingEntry("Other Database");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        fsCache.GetAllKeyValues().Count.ShouldEqual(1);
        context.ShardingEntryBackup.Count().ShouldEqual(1);
    }

    private class TestClassHolder { }
}
