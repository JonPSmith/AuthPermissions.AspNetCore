// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.SupportCode;
using Test.TestHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestAccessDatabaseInformation
{
    private readonly ITestOutputHelper _output;

    public TestAccessDatabaseInformation(ITestOutputHelper output)
    {
        _output = output;
    }

    private void ResetShardingSettingsFile()
    {
        var testData = new ShardingSettingsOption
        {
            ShardingDatabases = new List<DatabaseInformation>
            {
                new (){ Name = "Default Database", ConnectionName = "UnitTestConnection"},
                new (){ Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "UnitTestConnection" },
                new (){ Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = "Postgres" }
            }
        };
        var jsonString = JsonSerializer.Serialize(testData, new JsonSerializerOptions{ WriteIndented = true });
        var filepath = Path.Combine(TestData.GetTestDataDir(), AccessDatabaseInformation.ShardingSettingFilename);
        File.WriteAllText(filepath, jsonString);
    }
               

    [Fact]
    public void TestReadShardingSettingsFile()
    {
        //SETUP
        ResetShardingSettingsFile();
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir() };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon);

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

    [Theory]
    [InlineData("New Name", true)]
    [InlineData(null, false)]
    [InlineData("Default Database", false)]
    public void TestAddDatabaseInfoToJsonFile_TestName(string name, bool isValid)
    {
        //SETUP
        ResetShardingSettingsFile();
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir() };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon);

        //ATTEMPT
        var databaseInfo = new DatabaseInformation { Name = name, ConnectionName = "UnitTestConnection" };
        var status = service.AddDatabaseInfoToJsonFile(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        service.ReadShardingSettingsFile().Count.ShouldEqual(status.IsValid ? 4 : 3);
    }

    [Theory]
    [InlineData("PostgreSqlConnection", true)]
    [InlineData("BadConnectionName", false)]
    public void TestUpdateDatabaseInfoToJsonFile(string connectionName, bool isValid)
    {
        //SETUP
        ResetShardingSettingsFile();
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir() };
        var stubCon = new StubConnectionsService(this, !isValid);
        var service = new AccessDatabaseInformation(stubEnv, stubCon);

        //ATTEMPT
        var databaseInfo = new DatabaseInformation { Name = "Default Database", ConnectionName = connectionName };
        var status = service.UpdateDatabaseInfoToJsonFile(databaseInfo);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
    }

    [Theory]
    [InlineData("PostgreSql1", true)]
    [InlineData("Default Database", false)]
    public async Task TestDeleteDatabaseInfoToJsonFileAsync(string name, bool isValid)
    {
        //SETUP
        ResetShardingSettingsFile();
        var stubEnv = new StubWebHostEnvironment { ContentRootPath = TestData.GetTestDataDir() };
        var stubCon = new StubConnectionsService(this);
        var service = new AccessDatabaseInformation(stubEnv, stubCon);

        //ATTEMPT
        var status = await service.RemoveDatabaseInfoToJsonFileAsync(name);

        //VERIFY
        _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
    }
}