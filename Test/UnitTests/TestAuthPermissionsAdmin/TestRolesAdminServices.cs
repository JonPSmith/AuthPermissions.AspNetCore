// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestRolesAdminServices
    {


        [Fact]
        public async Task TestQueryRoles()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions{EnumPermissionsType = typeof(TestEnum)});

            //ATTEMPT
            var roles = service.QueryRoleToPermissions().ToList();

            //VERIFY
            roles.Count.ShouldEqual(3);
            roles.Select(x => x.RoleName).ShouldEqual(new[]{"Role1", "Role2", "Role3"});
        }

        [Theory]
        [InlineData("Role3", 1)]
        [InlineData("Role2", 2)]
        [InlineData("Role1", 3)]
        public async Task TestQueryUsersUsingThisRole(string roleName, int numAuthExpected)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions{EnumPermissionsType = typeof(TestEnum)});

            //ATTEMPT
            var authUsers = service.QueryUsersUsingThisRole(roleName).ToList();

            //VERIFY
            authUsers.Count.ShouldEqual(numAuthExpected);
        }

        [Theory]
        [InlineData("Two", true)]
        [InlineData("BadMemberName", false)]
        public async Task TestAddRoleToPermissionsAsyncOk(string enumMemberName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions { EnumPermissionsType = typeof(TestEnum) });

            //ATTEMPT
            var status = await service.AddRoleToPermissionsAsync("Role4", "another role", new[] { "One", enumMemberName, "Three" });

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            context.RoleToPermissions.Count().ShouldEqual(isValid ? 4 : 3);
            if (isValid)
            {
                context.RoleToPermissions.Single(x => x.RoleName == "Role4").ToString()
                    .ShouldEqual("Role4 (description = another role) has 3 permissions.");
                status.Message.ShouldEqual("Successfully added the new role Role4.");
            }
        }

        [Fact]
        public async Task TestAddRoleToPermissionsAsyncDuplicate()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions { EnumPermissionsType = typeof(TestEnum) });

            //ATTEMPT
            var status = await service.AddRoleToPermissionsAsync("Role2", "another role", new[] { "One", "Two", "Three" });

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a RoleToPermissions with a value: name = Role2");
        }


        [Theory]
        [InlineData("Role2", true)]
        [InlineData("BadRoleName", false)]
        public async Task UpdateRoleToPermissionsAsync(string roleName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions { EnumPermissionsType = typeof(TestEnum) });

            //ATTEMPT
            var status = await service.UpdateRoleToPermissionsAsync(roleName,  new[] { "One" }, "different description");

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            if (isValid)
            {
                context.RoleToPermissions.Single(x => x.RoleName == roleName).ToString()
                    .ShouldEqual("Role2 (description = different description) has 1 permissions.");
                status.Message.ShouldEqual("Successfully updated the role Role2.");
            }
        }

        [Theory]
        [InlineData("Role2", true)]
        [InlineData("BadRoleName", false)]
        public async Task DeleteRoleAsyncNoUsers(string roleName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions { EnumPermissionsType = typeof(TestEnum) });

            //ATTEMPT
            var status = await service.DeleteRoleAsync(roleName, false);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            if (isValid)
            {
                context.RoleToPermissions.SingleOrDefault(x => x.RoleName == roleName).ShouldBeNull();
                status.Message.ShouldEqual("Successfully deleted the role Role2.");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DeleteRoleAsyncWithUsers(bool removeFromUser)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions { EnumPermissionsType = typeof(TestEnum) });

            //ATTEMPT
            var status = await service.DeleteRoleAsync("Role2", removeFromUser);

            //VERIFY
            status.IsValid.ShouldEqual(removeFromUser);
            if (status.IsValid)
            {
                service.QueryUsersUsingThisRole("Role2").Count().ShouldEqual(0);
                status.Message.ShouldEqual("Successfully deleted the role Role2 and removed that role from 2 users.");
            }
        }
    }
}