// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupParts;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestSetupPartsSetupUsersService
    {
        private readonly ITestOutputHelper _output;

        public TestSetupPartsSetupUsersService(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddRolesToDatabaseIfEmpty()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new SetupUsersService(context);

            //ATTEMPT
            var status = await service.AddUsersToDatabaseIfEmpty(
                SetupHelpers.TestUserDefine());
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
        public async Task TestAddRolesToDatabaseIfEmptyNoRoleError()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new SetupUsersService(context);

            //ATTEMPT
            var status = await service.AddUsersToDatabaseIfEmpty(
                SetupHelpers.TestUserDefine(""));

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldStartWith("Line/index 1: The user User2 didn't have any roles.");
        }

        [Fact]
        public async Task TestAddRolesToDatabaseIfEmptyBadRole()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();

            context.ChangeTracker.Clear();

            var service = new SetupUsersService(context);

            //ATTEMPT
            var status = await service.AddUsersToDatabaseIfEmpty(
                SetupHelpers.TestUserDefine("Role99"));

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldStartWith("Line/index 2, char: 1: The role Role99 wasn't found in the auth database.");
        }

    }
}
