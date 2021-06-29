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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions{TenantType = TenantTypes.SingleTenant});

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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var authUser = await service.FindAuthUserByUserIdAsync("User1");

            //VERIFY
            authUser.ShouldNotBeNull();
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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var authUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");

            //VERIFY
            authUser.ShouldNotBeNull();
        }

        [Fact]
        public async Task TestSyncAndShowChangesAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var authenticationServiceFactory = new StubSyncAuthenticationUsersFactory();
            var service = new AuthUsersAdminService(context, authenticationServiceFactory, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var changes = await service.SyncAndShowChangesAsync();

            //VERIFY
            foreach (var synChange in changes)
            {
                _output.WriteLine(synChange.ToString());
            }
            changes.Select(x => x.FoundChange.ToString()).ShouldEqual(new []{ "Update", "Add", "Remove" });
        }

        [Fact]
        public async Task TestApplySyncChangesAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var authenticationServiceFactory = new StubSyncAuthenticationUsersFactory();
            var service = new AuthUsersAdminService(context, authenticationServiceFactory, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var changes = await service.SyncAndShowChangesAsync();

            //ATTEMPT
            var status = await service.ApplySyncChangesAsync(changes);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            _output.WriteLine(status.Message);
            context.ChangeTracker.Clear();
            var authUsers = context.AuthUsers.OrderBy(x => x.Email).ToList();
            foreach (var authUser in authUsers)
            {
                _output.WriteLine(authUser.ToString());
            }
            authUsers.Select(x => x.Email).ShouldEqual(new []{ "User1@gmail.com", "User2@gmail.com", "User99@gmail.com" });
            authUsers.Select(x => x.UserName).ShouldEqual(new[] { "first last 0", "new name", "user 99" });
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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var authUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");

            //ATTEMPT
            var status = await service.ChangeUserNameAndEmailAsync(authUser, "new user name", email);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("The email 'bad.email' is not a valid email.");
            else
            {
                context.ChangeTracker.Clear();
                var rereadUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");
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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var authUser = await service.FindAuthUserByEmailAsync("User2@gmail.com");

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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var authUser = await service.FindAuthUserByEmailAsync("User2@gmail.com");

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

            var service = new AuthUsersAdminService(context, null, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var authUser = await service.FindAuthUserByEmailAsync("User2@gmail.com");

            //ATTEMPT
            var status = await service.ChangeTenantToUserAsync(authUser, tenantName);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("Could not find the tenant Bad Tenant name");
        }
    }
}