// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

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
    public class TestConcurrency
    {
        //NOTE: Sqlite doesn't support concurrency support, but if needed it can be added
        //see https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html


        [Fact]
        public void TestUpdateRoleSqlServer()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            var initial = new RoleToPermissions("Test", null, "123");
            context.Add(initial);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            //ATTEMPT
            var entity = context.RoleToPermissions.Single();
            context.Database.ExecuteSqlInterpolated($"UPDATE authp.RoleToPermissions SET PackedPermissionsInRole = 'XYZ' WHERE RoleName = {initial.RoleName}");
            entity.Update("ABC");
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("Another user changed the RoleToPermissions with the name = Test. Please re-read the entity and add you change again.");
        }



    }
}