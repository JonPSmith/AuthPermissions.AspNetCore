// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SetupCode;
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
        [InlineData("One,Three", true)]
        [InlineData("Two,Four", false)]
        [InlineData("Seven", false)]
        [InlineData("  Seven  ", false)]
        [InlineData("One, Two, Three, Four", false)]
        public async Task TestAddRolesToDatabaseIfEmptyOneLineNoDescription(string permissionsCommaDelimited, bool valid)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(TestEnum);
            var service = new BulkLoadRolesService(context, authOptions);

            //ATTEMPT
            var status = await service.AddRolesToDatabaseAsync(new List<BulkLoadRolesDto>
                {
                    new ("Role1", null, permissionsCommaDelimited)
                });

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

            //ATTEMPT
            var status = await service.AddRolesToDatabaseAsync(AuthPSetupHelpers.TestRolesDefinition123);
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //VERIFY
            var roles = context.RoleToPermissions.OrderBy(x => x.RoleName).ToList();
            roles.Count.ShouldEqual(3);
            foreach (var role in roles)
            {
                _output.WriteLine(role.ToString());
            }
            roles[0].PackedPermissionsInRole.ShouldEqual($"{(char)1}");
            roles[1].PackedPermissionsInRole.ShouldEqual($"{(char)2}");
            roles[2].PackedPermissionsInRole.ShouldEqual($"{(char)3}");
        }
    }
}