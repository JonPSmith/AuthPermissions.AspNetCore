// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;

namespace Test.UnitTests
{
    /// <summary>
    /// This is a basic test that the <see cref="AuthPermissionsDbContext"/> is configured for SQL Server and Postgres side
    /// </summary>
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