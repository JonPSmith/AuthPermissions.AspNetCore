// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestSetupPartsSetupTenantService
    {
        private readonly ITestOutputHelper _output;

        public TestSetupPartsSetupTenantService(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseEmptyString()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync("", new AuthPermissionsOptions());

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseDuplicateTenantName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var lines = @"Tenant1
Tenant1
Tenant3";

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(lines, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors()
                .ShouldEqual(
                    $"There were tenants with duplicate names, they are: Tenant1");
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseSingleTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var lines = @"Tenant1
Tenant2
Tenant3";
            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(lines, authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            context.Tenants.Count().ShouldEqual(3);
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseSingleTenantDuplicate()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var lines = @"Tenant1
Tenant2
Tenant2
Tenant3
Tenant3";

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(lines, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors()
                .ShouldEqual($"There were tenants with duplicate names, they are: Tenant2{Environment.NewLine}Tenant3");
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };
            var lines = @"Company
Company | West Coast | 
Company | West Coast | SanFran
Company | West Coast | SanFran | Shop1
Company | West Coast | SanFran | Shop2
Company | West Coast | LA 
Company | West Coast | LA | Shop1
Company | West Coast | LA | Shop2";

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(lines, authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants)
            {
                _output.WriteLine(tenant.ToString());
            }
            context.Tenants.Count().ShouldEqual(8);
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenantBadName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };
            var lines = @"Company
Company | West Coast | 
Company | West Coast | San???
Company | XX Coast | SanFran | Shop1
Company | West Coast | SanFran | Shop2
Company | West Coast | LA 
Company | YY Coast | LA | Shop1
Company | West Coast | LA | Shop2";

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(lines, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse(); 
            status.Errors.Count.ShouldEqual(3);
            status.Errors[0].ToString().ShouldStartWith("The tenant Company | XX Coast | SanFran | Shop1 on line 3 parent Company | XX Coast | SanFran was not found");
            status.Errors[1].ToString().ShouldStartWith("The tenant Company | West Coast | SanFran | Shop2 on line 4 parent Company | West Coast | SanFran was not found");
            status.Errors[2].ToString().ShouldStartWith("The tenant Company | YY Coast | LA | Shop1 on line 6 parent Company | YY Coast | LA was not found");
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenantMissingName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };
            var lines = @"Company
Company | West Coast | SanFran
Company | West Coast | SanFran | Shop1
Company | West Coast | SanFran | Shop2";

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(lines, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.Errors.Count.ShouldEqual(3);
            status.Errors[0].ToString().ShouldStartWith("The tenant Company | West Coast | SanFran on line 1 parent Company | West Coast was not found.");
            status.Errors[1].ToString().ShouldStartWith("The tenant Company | West Coast | SanFran | Shop1 on line 2 parent Company | West Coast | SanFran was not found");
            status.Errors[2].ToString().ShouldStartWith("The tenant Company | West Coast | SanFran | Shop2 on line 3 parent Company | West Coast | SanFran was not found");
        }
    }
}