// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Example1.RazorPages.IndividualAccounts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestNonRegisterAddUserManager
{
    private readonly ITestOutputHelper _output;

    public TestNonRegisterAddUserManager(ITestOutputHelper output)
    {
        _output = output;
    }

    private ILogger<T> CreateLogger<T>()
        where T : class
    {
        return new LoggerFactory(
                new[] { new MyLoggerProviderActionOut(log => _output.WriteLine(log.Message))})
            .CreateLogger<T>();
    }

    [Theory]
    [InlineData("User2@gmail.com", false)]
    [InlineData("AnotherEmail", true)]
    public async Task TestCheckNoExistingAuthUserAsync_Email(string email, bool isValid)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        context.AddMultipleUsersWithRolesInDb();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { Email = email };

        context.ChangeTracker.Clear();
        //ATTEMPT
        var status = await service.CheckNoExistingAuthUserAsync(userData);

        //VERIFY
        status.IsValid.ShouldEqual(isValid);
    }

    [Theory]
    [InlineData("first last 2", false)]
    [InlineData("Another username", true)]
    public async Task TestCheckNoExistingAuthUserAsync_UserName(string userName, bool isValid)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        context.AddMultipleUsersWithRolesInDb();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { UserName = userName };

        //ATTEMPT
        var status = await service.CheckNoExistingAuthUserAsync(userData);

        //VERIFY
        status.IsValid.ShouldEqual(isValid);
    }

    [Fact]
    public async Task TestSetUserInfoAsyncOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { Email = "me@gmail.com", Roles = new(){"Role1","Role2"} };

        //ATTEMPT
        var status = await service.SetUserInfoAsync(userData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue();
        var readInfo = context.AddNewUserInfos.Single();
        readInfo.Email.ShouldEqual(userData.Email);
        readInfo.RolesNamesCommaDelimited.ShouldEqual("Role1,Role2");
        DateTime.UtcNow.Subtract(readInfo.CreatedAtUtc).TotalMilliseconds.ShouldBeInRange(0, 200);
    }

    [Fact]
    public async Task TestSetUserInfoAsync_TwoCallsSameOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
            EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "Role1", "Role2" } };

        (await service.SetUserInfoAsync(userData)).IsValid.ShouldBeTrue();
        context.ChangeTracker.Clear();
        //ATTEMPT

        var status = await service.SetUserInfoAsync(userData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue();
        var readInfo = context.AddNewUserInfos.Single();
        readInfo.Email.ShouldEqual(userData.Email);
        readInfo.RolesNamesCommaDelimited.ShouldEqual("Role1,Role2");
        DateTime.UtcNow.Subtract(readInfo.CreatedAtUtc).TotalMilliseconds.ShouldBeInRange(0, 500);
    }

    [Fact]
    public async Task TestSetUserInfoAsync_TwoCallsDifferentBad()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
            EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "Role1", "Role2" } };

        (await service.SetUserInfoAsync(userData)).IsValid.ShouldBeTrue();
        context.ChangeTracker.Clear();
        //ATTEMPT

        var newUserData = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "DifferentRoles"} };
        var status = await service.SetUserInfoAsync(newUserData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeFalse();
        _output.WriteLine(status.GetAllErrors());
    }

    [Fact]
    public async Task TestSetUserInfoAsync_TwoCallsDifferentOutsideTime()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
            EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "Role1", "Role2" } };

        (await service.SetUserInfoAsync(userData)).IsValid.ShouldBeTrue();
        context.ChangeTracker.Clear();
        //ATTEMPT

        service.TimeoutSecondsOnSameUserAdded = 0;
        var newUserData = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "DifferentRoles" } };
        var status = await service.SetUserInfoAsync(newUserData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var readInfo = context.AddNewUserInfos.Single();
        readInfo.Email.ShouldEqual(userData.Email);
        readInfo.RolesNamesCommaDelimited.ShouldEqual("DifferentRoles");
        DateTime.UtcNow.Subtract(readInfo.CreatedAtUtc).TotalMilliseconds.ShouldBeInRange(0, 200);
    }

    [Theory]
    [InlineData("User2@gmail.com", true)]
    [InlineData("AnotherEmail", false)]
    public async Task LoginVerificationAsyncOk(string email, bool isValid)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
            EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = new NonRegisterAddUserManager(context, CreateLogger<NonRegisterAddUserManager>());
        var userData = new AddNewUserDto { Email = email, Roles = new() { "Role1", "Role2" } };

        (await service.SetUserInfoAsync(userData)).IsValid.ShouldBeTrue();
        context.AddMultipleUsersWithRolesInDb();
        context.ChangeTracker.Clear();
        //ATTEMPT

        var status = await service.LoginVerificationAsync(null,null);

        //VERIFY
        status.IsValid.ShouldEqual(isValid);
    }
}