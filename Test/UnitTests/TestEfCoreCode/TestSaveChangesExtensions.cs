// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
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
            var status = context.SaveChangesWithUniqueCheck();

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
            var status = context.SaveChangesWithUniqueCheck();

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("There is already a RoleToPermissions with a value: name = BIG Name");
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
            var status = context.SaveChangesWithUniqueCheck();

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("There is already a RoleToPermissions with a value: name = Test1");
        }
    }
}