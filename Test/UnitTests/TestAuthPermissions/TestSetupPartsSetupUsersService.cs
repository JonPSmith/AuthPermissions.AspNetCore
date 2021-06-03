// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestSetupPartsSetupUsersRolesService
    {
        private readonly ITestOutputHelper _output;

        public TestSetupPartsSetupUsersRolesService(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmpty()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, null);

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(
                SetupHelpers.TestUserDefineWithUserId());
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //VERIFY
            var usersWithRoles = context.UserToRoles.ToList();
            foreach (var userWithRole in usersWithRoles)
            {
                _output.WriteLine(userWithRole.ToString());
            }

            usersWithRoles.Count.ShouldEqual(5);
            usersWithRoles[0].ToString().ShouldEqual("User User1 has role Role1");
            usersWithRoles[1].ToString().ShouldEqual("User User2 has role Role1");
            usersWithRoles[2].ToString().ShouldEqual("User User2 has role Role2");
            usersWithRoles[3].ToString().ShouldEqual("User User3 has role Role1");
            usersWithRoles[4].ToString().ShouldEqual("User User3 has role Role3");
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmptyWithIFindUserId()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, new MockIFindUserId());

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(
                SetupHelpers.TestUserDefineNoUserId());
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //VERIFY
            var usersWithRoles = context.UserToRoles.ToList();
            foreach (var userWithRole in usersWithRoles)
            {
                _output.WriteLine(userWithRole.ToString());
            }

            usersWithRoles.Count.ShouldEqual(5);
            usersWithRoles[0].ToString().ShouldEqual("User User1 has role Role1");
            usersWithRoles[1].ToString().ShouldEqual("User User2 has role Role1");
            usersWithRoles[2].ToString().ShouldEqual("User User2 has role Role2");
            usersWithRoles[3].ToString().ShouldEqual("User User3 has role Role1");
            usersWithRoles[4].ToString().ShouldEqual("User User3 has role Role3");
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmptyNoUserIdFail()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, null);

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(SetupHelpers.TestUserDefineNoUserId(null));

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Single().ToString().ShouldStartWith("Line/index 1: The user User2 didn't have a userId and the IFindUserIdService wasn't available.");
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmptyNoRoleError()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, null);

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(
                SetupHelpers.TestUserDefineWithUserId(""));

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldStartWith("Line/index 1: The user User2 didn't have any roles.");
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmptyBadRole()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, null);

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(
                SetupHelpers.TestUserDefineWithUserId("Role99"));

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldStartWith("Line/index 2, char: 1: The role Role99 wasn't found in the auth database.");
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmptySetupWithTenantsGood()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();
            context.SetupTenantsInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, null);

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(
                SetupHelpers.TestUserDefineWithTenants());

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var userToTenants = context.UserToTenants.ToList();
            foreach (var entity in userToTenants)
            {
                _output.WriteLine(entity.ToString());
            }
            context.UserToTenants.Count().ShouldEqual(2);
        }

        [Fact]
        public async Task TestAddUserRolesToDatabaseIfEmptySetupWithTenantsMissingTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();
            context.SetupTenantsInDb();

            context.ChangeTracker.Clear();

            var service = new BulkLoadUsersService(context, null);

            //ATTEMPT
            var status = await service.AddUsersRolesToDatabaseIfEmptyAsync(
                SetupHelpers.TestUserDefineWithTenants("Tenant99"));

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldStartWith("Line/index 1: The user User2 has a tenant name of Tenant99 which wasn't found in the auth database.");
        }

    }
}
