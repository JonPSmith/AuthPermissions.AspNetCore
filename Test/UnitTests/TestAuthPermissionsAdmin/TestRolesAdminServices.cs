// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestRolesAdminServices
    {

        private readonly ITestOutputHelper _output;

        private readonly AuthPermissionsOptions _authOptionsWithTestEnum =
            new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };

        public TestRolesAdminServices(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestQueryRolesNotMultiTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var roles = service.QueryRoleToPermissions().ToList();

            //VERIFY
            roles.Count.ShouldEqual(3);
            roles.Select(x => x.RoleName).ShouldEqual(new[]{"Role1", "Role2", "Role3"});
            roles.Last().PermissionNames.ShouldEqual(new List<string>{ "Three"});
        }

        [Theory]
        [InlineData(RoleTypes.Normal, true, 2)]
        [InlineData(RoleTypes.TenantAutoAdd, true, 2)]
        [InlineData(RoleTypes.TenantAdminAdd, true, 2)]
        [InlineData(RoleTypes.HiddenFromTenant, false, 2)]
        public void TestQueryRolesTenantUser(RoleTypes role2Type, bool hasTenant, int numRolesFound)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setupUser = context.SetupUserWithDifferentRoleTypes(role2Type, hasTenant);

            var service = new AuthRolesAdminService(context, new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel,
                InternalData = { EnumPermissionsType = typeof(TestEnum) }
            }, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var roles = service.QueryRoleToPermissions(setupUser.UserId).ToList();

            //VERIFY
            foreach (var role in roles)
            {
                _output.WriteLine($"{role.RoleName}: type = {role.RoleType}");
            }
            roles.Count.ShouldEqual(numRolesFound);
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

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

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

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var status = await service.CreateRoleToPermissionsAsync("Role4", new[] { "One", enumMemberName, "Three" }, "another role");

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
            {
                builder.UseExceptionProcessor();
                builder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            });
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            await context.SetupRolesInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var status = await service.CreateRoleToPermissionsAsync("Role2", new[] { "One", "Two", "Three" }, "another role");

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a Role with the name of 'Role2'.");
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

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var status = await service.UpdateRoleToPermissionsAsync(roleName,  new[] { "One" },
                "different description", RoleTypes.TenantAdminAdd);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            if (isValid)
            {
                context.ChangeTracker.Clear();
                var role = context.RoleToPermissions.Single(x => x.RoleName == roleName);
                role.ToString()
                    .ShouldEqual("Role2 (description = different description) has 1 permissions.");
                role.RoleType.ShouldEqual(RoleTypes.TenantAdminAdd);
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

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

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

            var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

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