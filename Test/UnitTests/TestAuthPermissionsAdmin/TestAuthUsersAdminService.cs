// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestAuthUsersAdminService
    {
        private readonly ITestOutputHelper _output;

        public TestAuthUsersAdminService(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestQueryAuthUsers()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions{TenantType = TenantTypes.SingleLevel});

            //ATTEMPT
            var users = service.QueryAuthUsers()
                .Include(x => x.UserRoles)
                .ToList();

            //VERIFY
            foreach (var authUser in users)
            {
                _output.WriteLine(authUser.ToString());
            }
            users.Count.ShouldEqual(3);
            users.OrderBy(x => x.UserId).Select(x => x.UserId).ShouldEqual(new[]{ "User1", "User2", "User3" });
            users.OrderBy(x => x.Email).Select(x => x.Email).ShouldEqual(new[] { "User1@gmail.com", "User2@gmail.com", "User3@gmail.com" });
        }


        [Fact]
        public async Task TestFindAuthUserByUserIdAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var status = await service.FindAuthUserByUserIdAsync("User1");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldNotBeNull();
        }

        [Fact]
        public async Task TestFindAuthUserByEmailAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var status = await service.FindAuthUserByEmailAsync("User1@gmail.com");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldNotBeNull();
        }

        [Theory]
        [InlineData("User1@gmail.com", true)]
        [InlineData("bad.email", false)]
        public async Task TestChangeEmailAsyncOk(string email, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });
            var authUser = (await service.FindAuthUserByEmailAsync("User1@gmail.com")).Result;

            //ATTEMPT
            var status = await service.UpdateUserAsync(authUser.UserId, email, "new user name",
                authUser.UserRoles.Select(x => x.RoleName).ToList(), null);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("The email 'bad.email' is not a valid email.");
            else
            {
                context.ChangeTracker.Clear();
                var rereadUser = (await service.FindAuthUserByEmailAsync("User1@gmail.com")).Result;
                rereadUser.UserName.ShouldEqual("new user name");
            }
        }

        [Theory]
        [InlineData("Role1", true)]
        [InlineData("Role3", true)]
        [InlineData("Role99", false)]
        public async Task TestAddRoleToUser(string roleName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });
            var authUser = (await service.FindAuthUserByEmailAsync("User2@gmail.com")).Result;

            //ATTEMPT
            var status = await service.AddRoleToUser(authUser, roleName);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("Could not find the role Role99");
        }

        [Theory]
        [InlineData("Role1", true)]
        [InlineData("Role3", true)]
        [InlineData("Role99", false)]
        public async Task TestRemoveRoleToUser(string roleName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });
            var authUser = (await service.FindAuthUserByEmailAsync("User2@gmail.com")).Result;

            //ATTEMPT
            var status = await service.RemoveRoleToUser(authUser, roleName);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("Could not find the role Role99");
        }

        [Theory]
        [InlineData("Tenant1", true)]
        [InlineData("Bad Tenant name", false)]
        public async Task TestChangeTenantToUserAsync(string tenantName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });
            var authUser = (await service.FindAuthUserByEmailAsync("User2@gmail.com")).Result;

            //ATTEMPT
            var status = await service.UpdateUserAsync(authUser.UserId, authUser.Email, authUser.UserName,
                authUser.UserRoles.Select(x => x.RoleName).ToList(), tenantName);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("A tenant with the name 'Bad Tenant name' wasn't found.");
        }


        [Theory]
        [InlineData("User2", true)]
        [InlineData("bad userId", false)]
        public async Task TestDeleteUserAsync(string userId, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var status = await service.DeleteUserAsync(userId);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("Could not find the user you were looking for.");
        }
    }
}