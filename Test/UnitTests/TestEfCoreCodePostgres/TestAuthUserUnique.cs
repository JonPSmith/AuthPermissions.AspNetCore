// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Test.Helpers;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestEfCoreCodePostgres
{
    public class TestAuthUserUnique
    {
        [Fact]
        public void TestAddAuthUserNullEmail()
        {
            //SETUP
            var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>(builder =>
            {
                builder.UseExceptionProcessor();
                builder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            });

            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            //ATTEMPT
            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", null, "userName"));
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
        }

        [Fact]
        public void TestAddAuthUserNullUserName()
        {
            //SETUP
            var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>(builder =>
            {
                builder.UseExceptionProcessor();
                builder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            });
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", "j@gmail.com", "userName"));
            var status = context.SaveChangesWithChecks("en".SetupAuthPLoggingLocalizer().DefaultLocalizer);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
        }

        [Fact]
        public void TestAddAuthUserNullEmailAndUserName()
        {
            //SETUP
            var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>(builder =>
            {
                builder.UseExceptionProcessor();
                builder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            });
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            //ATTEMPT
            var ex = Assert.Throws<AuthPermissionsBadDataException>(() =>
                AuthPSetupHelpers.CreateTestAuthUserOk("123", null, null));

            //VERIFY
            ex.Message.ShouldEqual("The Email and UserName can't both be null.");
        }

    }
}