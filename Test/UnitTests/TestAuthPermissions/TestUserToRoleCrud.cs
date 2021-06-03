// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.DataLayer.EfCode;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestUserToRoleCrud
    {
        private readonly ITestOutputHelper _output;

        public TestUserToRoleCrud(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public async Task TestReadUserToRoles()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();
            context.AddUserToRoleInDb(3);
            context.ChangeTracker.Clear();

            var service = new UserToRoleCrud(context);

            //ATTEMPT
            var roles = await service.GetUsersRoleToPermissionsAsync("User1");

            //VERIFY
            roles.ShouldEqual(new List<string>{"Role1", "Role2", "Role3"});
        }

        [Theory]
        [InlineData("Role3", false)]
        [InlineData("Role2", true)]
        [InlineData("Role99", true)]
        public async Task TestAddRoleToUserAsync(string roleName, bool shouldError)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();
            context.AddUserToRoleInDb(2);
            context.ChangeTracker.Clear();

            var service = new UserToRoleCrud(context);

            //ATTEMPT
            var status = await service.AddRoleToUserAsync("User1", roleName, "xx");

            //VERIFY
            status.HasErrors.ShouldEqual(shouldError);
            _output.WriteLine(status.GetAllErrors() ?? status.Message);
        }

        [Theory]
        [InlineData("Role3", false)]
        [InlineData("Role99", true)]
        public async Task TestRemoveRoleFromUserAsync(string roleName, bool shouldError)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupRolesInDb();
            context.AddUserToRoleInDb(3);
            context.ChangeTracker.Clear();

            var service = new UserToRoleCrud(context);

            //ATTEMPT
            var status = await service.RemoveRoleFromUserAsync("User1", roleName);

            //VERIFY
            _output.WriteLine(status.GetAllErrors() ?? status.Message);
            status.HasErrors.ShouldEqual(shouldError);
        }
    }
}