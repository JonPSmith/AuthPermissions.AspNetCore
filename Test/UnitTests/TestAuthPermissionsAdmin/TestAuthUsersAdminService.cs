// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using EntityFramework.Exceptions.SqlServer;
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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions{TenantType = TenantTypes.SingleTenant});

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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var authUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");

            //VERIFY
            authUser.ShouldNotBeNull();
        }

        [Theory]
        [InlineData("Role2", true)]
        [InlineData("Role99", false)]
        public async Task TestAddNewUserWithRolesAsyncOk(string roleName, bool isValid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var status = await service.AddNewUserWithRolesAsync("User9","User9@gmail.com", "User9 name",
                new List<string>{ "Role1", roleName });

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("The following role names were not found: Role99");
        }

        [Fact]
        public async Task TestAddNewUserWithRolesAsyncDuplicate()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var status = await service.AddNewUserWithRolesAsync("User2", "User2@gmail.com", "User2 name",
                new List<string> { "Role1" });

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a AuthUser with a value: name = User2 name");
        }

        [Fact]
        public async Task TestChangeUserNameAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var authUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");

            //ATTEMPT
            var status = await service.ChangeUserNameAsync(authUser, "new user name");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            _output.WriteLine(status.Message);
            context.ChangeTracker.Clear();
            var rereadUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");
            rereadUser.UserName.ShouldEqual("new user name");
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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
            var authUser = await service.FindAuthUserByEmailAsync("User1@gmail.com");

            //ATTEMPT
            var status = await service.ChangeEmailAsync(authUser, email);

            //VERIFY
            status.IsValid.ShouldEqual(isValid);
            _output.WriteLine(status.Message);
            if (!isValid)
                status.GetAllErrors().ShouldEqual("The email 'bad.email' is not a valid email.");
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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
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

            var service = new AuthUsersAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });
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