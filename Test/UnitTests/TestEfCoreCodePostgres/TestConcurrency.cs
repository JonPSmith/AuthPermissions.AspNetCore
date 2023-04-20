// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Test.Helpers;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.Attributes;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestEfCoreCodePostgres
{
    public class TestConcurrency
    {
        private readonly ITestOutputHelper _output;

        public TestConcurrency(ITestOutputHelper output)
        {
            _output = output;
        }


        //NOTE: Sqlite doesn't support concurrency support, but if needed it can be added
        //see https://www.bricelam.net/2020/08/07/sqlite-and-efcore-concurrency-tokens.html

        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //This doesn't work and I don't why
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        [RunnableInDebugOnly]
        public void TestUpdateRolePostgreSql()
        {
            //SETUP
            var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            var initial = new RoleToPermissions("Test", null, "123");
            context.Add(initial);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            //ATTEMPT
            var entity = context.RoleToPermissions.Single();
            using(var innerContext = new AuthPermissionsDbContext(options))
            {
                var innerEntity = context.RoleToPermissions.Single();
                innerEntity.Update("XYZ");
                context.SaveChanges();
            }
            entity.Update("ABC");
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("Another user changed the RoleToPermissions with the name = Test. Please re-read the entity and add you change again.");
        }



    }
}