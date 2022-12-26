// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestEfCoreCodeSqlServer
{
    public class TestSaveChangesExtensions
    {
        [Fact]
        public void TestSaveChangesWithUniqueCheckNoError()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder => 
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            //ATTEMPT
            context.Add(new RoleToPermissions("Test", null, "x"));
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
        }

        [Fact]
        public void TestSaveChangesWithUniqueCheckSingleDupError()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            context.Add(new RoleToPermissions("BIG Name", null, "x"));
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //ATTEMPT
            context.Add(new RoleToPermissions("BIG Name", null, "x"));
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("There is already a RoleToPermissions with a value: name = BIG Name");
        }

        [Fact]
        public void TestSaveChangesWithUniqueAuthUserWhereEmailAndUserNameAreDifferent()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", "first@gmail.com", "first"));
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //ATTEMPT
            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", "second@gmail.com", "second"));
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("There is already a AuthUser with a value: name = second@gmail.com or second");
        }

        [Fact]
        public void TestSaveChangesWithUniqueCheckTwoDupErrors()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            context.Add(new RoleToPermissions("Test1", null, "x"));
            context.Add(new RoleToPermissions("Test2", null, "x"));
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //ATTEMPT
            context.Add(new RoleToPermissions("Test1", null, "x"));
            context.Add(new RoleToPermissions("Test2", null, "x"));
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("There is already a RoleToPermissions with a value: name = Test1");
        }

        //---------------------------------------------
        //concurrency checks

        [Fact]
        public void TestSaveChangesWithConcurrencyCheck()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            context.Add(new RoleToPermissions("Test", null, "x"));
            context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            context.ChangeTracker.Clear();

            //ATTEMPT
            var role = context.RoleToPermissions.Single();
            role.Update("y", "new desc");
            context.Database.ExecuteSqlRaw(
                "UPDATE authp.RoleToPermissions SET Description = N'concurrent desc' WHERE RoleName = N'Test'");
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("Another user changed the RoleToPermissions with the name = Test. Please re-read the entity and add you change again.");
        }
    }
}