// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestEfCoreCode
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
            var status = context.SaveChangesWithChecks();

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
            var status = context.SaveChangesWithChecks();

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

            context.Add(new AuthUser("123", "first@gmail.com", "first", new List<RoleToPermissions>()));
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //ATTEMPT
            context.Add(new AuthUser("123", "second@gmail.com", "second", new List<RoleToPermissions>()));
            var status = context.SaveChangesWithChecks();

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
            var status = context.SaveChangesWithChecks();

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
            context.SaveChangesWithChecks();

            context.ChangeTracker.Clear();

            //ATTEMPT
            var role = context.RoleToPermissions.Single();
            role.Update("y", "new desc");
            context.Database.ExecuteSqlRaw(
                "UPDATE authp.RoleToPermissions SET Description = N'concurrent desc' WHERE RoleName = N'Test'");
            var status = context.SaveChangesWithChecks();

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("Another user changed the RoleToPermissions with the name = Test. Please re-read the entity and add you change again.");
        }
    }
}