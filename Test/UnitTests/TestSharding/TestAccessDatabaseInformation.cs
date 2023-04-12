// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Test.Helpers;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSharding;

public class TestAccessDatabaseInformation
{
    private readonly ITestOutputHelper _output;

    public TestAccessDatabaseInformation(ITestOutputHelper output)
    {
        _output = output;
    }

    private static AuthPermissionsOptions FormAuthOptionsForSharding(AuthPDatabaseTypes dbType = AuthPDatabaseTypes.SqlServer)
    {
        var options = new AuthPermissionsOptions
        {
            SecondPartOfShardingFile = "Test",
            InternalData =
            {
                AuthPDatabaseType = dbType
            }
        };
        return options;
    }


    private static void ResetShardingSettingsFile()
    {
        var testData = new ShardingSettingsOption
        {
            ShardingDatabases = new List<DatabaseInformation>
            {
                new (){ Name = "Default Database", ConnectionName = "UnitTestConnection", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer)},
                new (){ Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "UnitTestConnection", DatabaseType = nameof(AuthPDatabaseTypes.SqlServer) },
                new (){ Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = nameof(AuthPDatabaseTypes.Postgres) }
            }
        };
        var jsonString = JsonSerializer.Serialize(testData, new JsonSerializerOptions { WriteIndented = true });
        var filepath = Path.Combine(TestData.GetTestDataDir(), "shardingsettings.Test.json");
        File.WriteAllText(filepath, jsonString);
    }

    [Theory]
    [InlineData(null, "shardingsettings.json")]
    [InlineData("Test", "shardingsettings.Test.json")]
    public void TestAccessDatabaseInformationNoSecondPart(string secondPart, string expectedFileName)
    {
        //SETUP

        //ATTEMPT
        var shardingFilename = AuthPermissionsOptions.FormShardingSettingsFileName(secondPart);

        //VERIFY
        shardingFilename.ShouldEqual(expectedFileName);
    }

    [Fact]
    public void TestReadShardingSettingsFile()
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir(), EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
           FormAuthOptionsForSharding(), "en".SetupAuthPLoggingLocalizer());
        //ATTEMPT
        var databaseInfo = service.ReadShardingSettingsFile();

        //VERIFY
        foreach (var databaseInformation in databaseInfo)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        databaseInfo.Count.ShouldEqual(3);
        databaseInfo[0].ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
        databaseInfo[1].ToString().ShouldEqual("Name: Other Database, DatabaseName: MyDatabase1, ConnectionName: UnitTestConnection, DatabaseType: SqlServer");
        databaseInfo[2].ToString().ShouldEqual("Name: PostgreSql1, DatabaseName: StubTest, ConnectionName: PostgreSqlConnection, DatabaseType: Postgres");
    }

    [Fact]
    public void TestReadShardingSettingsFile_NoFile()
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir() + "DummyDir\\", EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
            FormAuthOptionsForSharding(), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var databaseInfo = service.ReadShardingSettingsFile();

        //VERIFY
        foreach (var databaseInformation in databaseInfo)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        databaseInfo.Count.ShouldEqual(1);
        databaseInfo.Single().ToString().ShouldEqual("Name: Default Database, DatabaseName:  < null > , ConnectionName: DefaultConnection, DatabaseType: SqlServer");
    }

    [Theory]
    [InlineData("New Name", true)]
    [InlineData(null, false)]
    [InlineData("Default Database", false)]
    public void TestAddDatabaseInfoToJsonFile_TestName(string name, bool isValid)
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir(), EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
            FormAuthOptionsForSharding(), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            DatabaseType = nameof(AuthPDatabaseTypes.SqlServer), Name = name, ConnectionName = "UnitTestConnection"
        };
        var status = service.AddDatabaseInfoToJsonFile(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        service.ReadShardingSettingsFile().Count.ShouldEqual(status.IsValid ? 4 : 3);
    }

    [Fact]
    public void TestUpdateDatabaseInfoToJsonFile()
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir(), EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
            FormAuthOptionsForSharding(), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var databaseInfo = new DatabaseInformation
        {
            DatabaseType = nameof(AuthPDatabaseTypes.SqliteInMemory),
            Name = "Default Database", ConnectionName = "PostgreSqlConnection"
        };
        var status = service.UpdateDatabaseInfoToJsonFile(databaseInfo);

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
    }

    [Theory]
    [InlineData("PostgreSql1", true)]
    [InlineData("Default Database", false)]
    public async Task TestDeleteDatabaseInfoToJsonFileAsync(string name, bool isValid)
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir(), EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
            FormAuthOptionsForSharding(), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.RemoveDatabaseInfoToJsonFileAsync(name);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
    }

    //------------------------------------------------------------------
    //Check DistributedLock

    [Fact]
    public void TestAddDatabaseInfoToJsonFile_SqlServerLock()
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir(), EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
            FormAuthOptionsForSharding(), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        Parallel.ForEach(new string[] { "Name1", "Name2", "Name3" },
            name =>
            {
                var databaseInfo = new DatabaseInformation
                {
                    DatabaseType = nameof(AuthPDatabaseTypes.SqlServer),
                    Name = name, DatabaseName = $"Database{name}", ConnectionName = "UnitTestConnection"
                };
                var status = service.AddDatabaseInfoToJsonFile(databaseInfo);
                status.IsValid.ShouldBeTrue();
            });


        //VERIFY
        var databaseInfo = service.ReadShardingSettingsFile();
        foreach (var databaseInformation in databaseInfo)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        service.ReadShardingSettingsFile().Count.ShouldEqual(6);
    }

    [Fact]
    public void TestAddDatabaseInfoToJsonFile_PostgresLock()
    {
        //SETUP
        ResetShardingSettingsFile();
        var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir(), EnvironmentName = "Test" };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon, context,
            FormAuthOptionsForSharding(AuthPDatabaseTypes.Postgres), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        Parallel.ForEach(new string[] { "Name1", "Name2", "Name3" },
            name =>
            {
                var databaseInfo = new DatabaseInformation
                {
                    DatabaseType = nameof(AuthPDatabaseTypes.Postgres),
                    Name = name, DatabaseName = $"Database{name}", ConnectionName = "UnitTestConnection"
                };
                var status = service.AddDatabaseInfoToJsonFile(databaseInfo);
                status.IsValid.ShouldBeTrue();
            });

        //VERIFY
        var databaseInfo = service.ReadShardingSettingsFile();
        foreach (var databaseInformation in databaseInfo)
        {
            _output.WriteLine(databaseInformation.ToString());
        }
        service.ReadShardingSettingsFile().Count.ShouldEqual(6);
    }
}