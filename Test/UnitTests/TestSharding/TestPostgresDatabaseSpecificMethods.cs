// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using StatusGeneric;
using Test.Helpers;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;
using Npgsql;

namespace Test.UnitTests.TestSharding;

public class TestPostgresDatabaseSpecificMethods
{
    private readonly ITestOutputHelper _output;

    public TestPostgresDatabaseSpecificMethods(ITestOutputHelper output)
    {
        _output = output;
    }

    private ShardingEntry SetupDatabaseInformation(bool nameIsNull)
    {
        return new ShardingEntry
        {
            Name = "EntryName",
            DatabaseType = nameof(AuthPDatabaseTypes.SqlServer),
            DatabaseName = nameIsNull ? null : "TestDatabase",
            ConnectionName = "DefaultConnection"
        };
    }

    [Theory]
    [InlineData(true, "OriginalName")]
    [InlineData(false, "TestDatabase")]
    public void TestSetDatabaseInConnectionStringOk(bool nullName, string dbName)
    {
        //SETUP
        var service = new PostgresDatabaseSpecificMethods();

        //ATTEMPT
        var connectionString = service.FormShardingConnectionString(SetupDatabaseInformation(nullName),
            "host=127.0.0.1;Database=OriginalName;Username=xxx;Password=yyy");

        //VERIFY
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.Database.ShouldEqual(dbName);
    }

    [Fact]
    public void TestSetDatabaseInConnectionStringBad()
    {
        //SETUP
        var service = new PostgresDatabaseSpecificMethods();

        //ATTEMPT
        var ex = Assert.Throws<AuthPermissionsException>(() => 
            service.FormShardingConnectionString(SetupDatabaseInformation(true),
            "host=127.0.0.1;Username=xxx;Password=yyy"));

        //VERIFY
        ex.Message.ShouldEqual("The DatabaseName can't be null or empty when the connection string doesn't have a database defined.");
    }

    [Fact]
    public void TestChangeDatabaseInformationWithinDistributedLock()
    {
        //SETUP
        var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new PostgresDatabaseSpecificMethods();
        var logs = new ConcurrentStack<string>();

        //ATTEMPT
        Parallel.ForEach(new string[] { "Name1", "Name2", "Name3" },
            name =>
            {
                var status = service.ChangeDatabaseInformationWithinDistributedLock(context.Database.GetConnectionString(),
                    () =>
                    {
                        logs.Push(name);
                        Thread.Sleep(10);
                        return new StatusGenericHandler();
                    });
                status.IsValid.ShouldBeTrue();
            });

        //VERIFY
        foreach (var log in logs)
        {
            _output.WriteLine(log);
        }
        logs.OrderBy(x => x).ToArray().ShouldEqual(new string[] { "Name1", "Name2", "Name3" });
    }

    [Fact]
    public async Task TestChangeDatabaseInformationWithinDistributedLockAsync()
    {
        //SETUP
        var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new PostgresDatabaseSpecificMethods();
        var logs = new ConcurrentStack<string>();

        async Task TaskAsync(int i)
        {
            var status = service.ChangeDatabaseInformationWithinDistributedLock(context.Database.GetConnectionString(),
                () =>
                {
                    logs.Push(i.ToString());
                    Thread.Sleep(10);
                    return new StatusGenericHandler();
                });
        }

        //ATTEMPT
        await 3.NumTimesAsyncEnumerable().AsyncParallelForEach(TaskAsync);

        //VERIFY
        foreach (var log in logs)
        {
            _output.WriteLine(log);
        }
        logs.OrderBy(x => x).ToArray().ShouldEqual(new string[] { "1", "2", "3" });
    }

}