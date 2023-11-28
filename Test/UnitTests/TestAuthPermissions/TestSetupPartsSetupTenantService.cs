// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using Test.TestHelpers;
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
        public async Task TestAddTenantsToDatabaseSingleTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(AuthPSetupHelpers.GetSingleTenant123(), authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            context.Tenants.Count().ShouldEqual(3);
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseSingleTenantNull()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(null, authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseSingleTenantDuplicateTenantName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var tenantDefine = AuthPSetupHelpers.GetSingleTenant123();
            tenantDefine.Add(new("Tenant1"));

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDefine, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors()
                .ShouldEqual("There is already a Tenant with the name 'Tenant1'");
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(AuthPSetupHelpers.GetHierarchicalDefinitionCompany(), authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants)
            {
                _output.WriteLine(tenant.ToString());
            }
            context.Tenants.Count().ShouldEqual(9);
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenantDuplicateName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };
            var tenantDef = AuthPSetupHelpers.GetHierarchicalDefinitionCompany();
            tenantDef.Add(new ("Company"));

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDef, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse(); 
            status.Errors.Count.ShouldEqual(1);
            status.Errors.Single().ToString().ShouldEqual("There is already a Tenant with a value: name = Company");
        }
    }
}