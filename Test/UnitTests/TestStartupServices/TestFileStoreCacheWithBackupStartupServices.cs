// Copyright (c) 2024 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.


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

namespace Test.UnitTests.TestStartupServices;

public class TestFileStoreCacheWithBackupStartupServices
{

    private readonly ITestOutputHelper _output;

    public TestFileStoreCacheWithBackupStartupServices(ITestOutputHelper output)
    {
        _output = output;
    }

    public List<LogOutput> Logs { get; private set; } = new List<LogOutput>();

    [Fact]
    public void TestSetupOfStartupService()
    {
        //SETUP
        var serviceProvider = SetupServiceToTest(true);
        var startupService = new StartupServiceShardingBackup();

        //ATTEMPT
        startupService.ApplyYourChangeAsync(serviceProvider);

        //VERIFY


    }

    //----------------------------------------------------------------------
    //support methods 


    private ServiceProvider SetupServiceToTest(bool hybridMode)
    {
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        AuthDbContext = new AuthPermissionsDbContext(options);
        //This has all the services that the StartupServiceShardingBackup needs

        var services = new ServiceCollection();
        services.AddSingleton<IDistributedFileStoreCacheClass>(new StubFileStoreCacheClass());
        services.AddSingleton<AuthPermissionsDbContext>(AuthDbContext);
        services.AddSingleton(new AuthPermissionsOptions());
        services.AddSingleton(new ShardingEntryOptions(hybridMode));
        services.AddLogging(x => new LoggerFactory(
                    new[] { new MyLoggerProviderActionOut(l => Logs.Add(l)) })
                .CreateLogger<GetSetShardingEntriesFileStoreCache>());
        var serviceProvider = services.BuildServiceProvider();

        //Now we get the services 
        StubFsCache = serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();

        AuthDbContext.Database.EnsureClean();
        StubFsCache.ClearAll();

        //Now we add the test ShardingEntries and the ShardingEntryBackup database
        var testEntries = new List<ShardingEntry>
        {
            new (){ Name = "Default Database", ConnectionName = "UnitTestConnection", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer)},
            new (){ Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "AnotherConnectionString", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer) },
            new (){ Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = nameof(AuthPDatabaseTypes.PostgreSQL) }
        };
        testEntries.ForEach(x => StubFsCache.SetClass(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(x.Name), x));
        AuthDbContext.ShardingEntryBackup.AddRange(testEntries);
        AuthDbContext.SaveChanges();

        return serviceProvider;
    }

     private AuthPermissionsDbContext AuthDbContext { get; set; }
     private IDistributedFileStoreCacheClass StubFsCache { get; set; }
}