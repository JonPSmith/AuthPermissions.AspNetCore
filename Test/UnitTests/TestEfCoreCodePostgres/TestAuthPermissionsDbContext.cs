// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Test.Helpers;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestEfCoreCodePostgres
{
    public class TestAuthPermissionsDbContext
    {
        [Fact]
        public void TestSqlServer()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
            {
                builder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            });
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //ATTEMPT
            context.Add(new RoleToPermissions("Test", null, "123"));
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
        }

        [Fact]
        public void TestPostgres()
        {
            //SETUP
            var options = this.CreatePostgreSqlUniqueClassOptions<AuthPermissionsDbContext>(builder =>
            {
                builder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            });
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //ATTEMPT
            context.Add(new RoleToPermissions("Test", null, "123"));
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
        }

    }
}