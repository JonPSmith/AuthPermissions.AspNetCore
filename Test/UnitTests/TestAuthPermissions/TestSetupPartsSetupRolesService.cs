// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.EfCode;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestSetupPartsSetupRolesService
    {
        private readonly ITestOutputHelper _output;

        public TestSetupPartsSetupRolesService(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("Role1: One,Three", true)]
        [InlineData("Role1: Two,Four", false)]
        [InlineData("Role1: Seven", false)]
        [InlineData("Role1    : Seven", false)]
        [InlineData("Role1: One, Two, Three, Four", false)]
        [InlineData("Role1:", false)]
        public async Task TestAddRolesToDatabaseIfEmptyOneLineNoDescription(string line, bool valid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(TestEnum);
            var service = new BulkLoadRolesService(context, authOptions);

            //ATTEMPT
            var status = await service.AddRolesToDatabaseAsync(line);

            //VERIFY
            if (!status.IsValid)
            {
                _output.WriteLine(status.GetAllErrors());
            }
            status.IsValid.ShouldEqual(valid);
        }

        [Theory]
        [InlineData("Role1|Description|: One,Three", true)]
        [InlineData("Role1|Description: Two", false)]
        [InlineData("Role1 Description|: Two", false)]
        public async Task TestAddRolesToDatabaseIfEmptyOneLineWithDescription(string line, bool valid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(TestEnum);
            var service = new BulkLoadRolesService(context, authOptions);

            //ATTEMPT
            var status = await service.AddRolesToDatabaseAsync(line);

            //VERIFY
            if (!status.IsValid)
            {
                _output.WriteLine(status.GetAllErrors());
            }
            status.IsValid.ShouldEqual(valid);
        }

        [Fact]
        public async Task TestAddRolesToDatabaseIfEmpty()
        {
            //SETUP
            //NOTE: A zero in a string causes read problems on SQLite
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            //var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();
            //context.Database.EnsureClean();

            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(TestEnum);
            var service = new BulkLoadRolesService(context, authOptions);

            var lines = @"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role with space: One";

            //ATTEMPT
            var status = await service.AddRolesToDatabaseAsync(lines);
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //VERIFY
            var roles = context.RoleToPermissions.ToList();
            roles.Count.ShouldEqual(3);
            foreach (var role in roles)
            {
                _output.WriteLine(role.ToString());
            }
            roles[0].PackedPermissionsInRole.ShouldEqual($"{(char)1}{(char)3}");
            roles[1].PackedPermissionsInRole.ShouldEqual($"{(char)1}{(char)2}{(char)3}");
            roles[2].PackedPermissionsInRole.ShouldEqual($"{(char)1}");
        }
    }
}