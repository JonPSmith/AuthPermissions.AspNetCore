// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestSetupPartsSetupTenantService_TenantRoles
    {
        private readonly ITestOutputHelper _output;

        public TestSetupPartsSetupTenantService_TenantRoles(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddSingleTenantsWithRoles()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var roleAutoAdd = new RoleToPermissions("RoleAutoAdd", null, $"{(char)2}{(char)3}", RoleTypes.TenantAutoAdd);
            var roleAdminAdd = new RoleToPermissions("RoleAdminAdd", null, $"{(char)1}{(char)3}", RoleTypes.TenantAdminAdd);
            context.AddRange(roleAutoAdd, roleAdminAdd);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var tenantDefinitions = new List<BulkLoadTenantDto>
            {
                new("Tenant1", "RoleAutoAdd", null),
                new("Tenant2", "RoleAdminAdd", null),
                new("Tenant3", "RoleAutoAdd, RoleAdminAdd", null),
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDefinitions, authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            var createdTenants = context.Tenants.Include(x => x.TenantRoles).ToList();
            createdTenants.Count.ShouldEqual(3);
            createdTenants[0].TenantRoles.Select(x => x.RoleName).ShouldEqual(new []{ "RoleAutoAdd"});
            createdTenants[1].TenantRoles.Select(x => x.RoleName).ShouldEqual(new[] { "RoleAdminAdd" });
            createdTenants[2].TenantRoles.Select(x => x.RoleName).ShouldEqual(new[] { "RoleAdminAdd", "RoleAutoAdd" });
        }

        [Fact]
        public async Task TestAddSingleTenantsWithRolesBadRoleType()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var normalRole = new RoleToPermissions("NormalRole", null, $"{(char)2}{(char)3}");
            var roleAdminAdd = new RoleToPermissions("RoleAdminAdd", null, $"{(char)1}{(char)3}", RoleTypes.TenantAdminAdd);
            context.AddRange(normalRole, roleAdminAdd);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var tenantDefinitions = new List<BulkLoadTenantDto>
            {
                new("Tenant1", "RoleAdminAdd", null),
                new("Tenant2", "NormalRole", null),
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDefinitions, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("Tenant 'Tenant2': the role called 'NormalRole' was not found. Either it is misspent " +
                                              "or the RoleType must be TenantAutoAdd or TenantAdminAdd");
        }

        [Fact]
        public async Task TestAddSingleTenantsWithRolesBadRoleName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel
            };
            var normalRole = new RoleToPermissions("NormalRole", null, $"{(char)2}{(char)3}");
            var roleAdminAdd = new RoleToPermissions("RoleAdminAdd", null, $"{(char)1}{(char)3}", RoleTypes.TenantAdminAdd);
            context.AddRange(normalRole, roleAdminAdd);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var tenantDefinitions = new List<BulkLoadTenantDto>
            {
                new("Tenant1", "RoleAdminAdd", null),
                new("Tenant2", "BadName", null),
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDefinitions, authOptions);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("Tenant 'Tenant2': the role called 'BadName' was not found. Either it is misspent " +
                                              "or the RoleType must be TenantAutoAdd or TenantAdminAdd");
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenantRolesTrickleDownOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var roleAutoAdd = new RoleToPermissions("RoleAutoAdd", null, $"{(char)2}{(char)3}", RoleTypes.TenantAutoAdd);
            var roleAdminAdd = new RoleToPermissions("RoleAdminAdd", null, $"{(char)1}{(char)3}", RoleTypes.TenantAdminAdd);
            context.AddRange(roleAutoAdd, roleAdminAdd);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var tenantDefinitions = new List<BulkLoadTenantDto>
            {
                new("Tenant1", "RoleAutoAdd", new BulkLoadTenantDto[]
                {
                    new("Tenant1-level1", null, new BulkLoadTenantDto[]
                    {
                        new("Tenant1-level2")
                    })
                }),
                new("Tenant2", "RoleAutoAdd, RoleAdminAdd", new BulkLoadTenantDto[]
                {
                    new("Tenant2-level1")
                })
            };

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDefinitions, authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            context.ChangeTracker.Clear();
            var createdTenants = context.Tenants.Include(x => x.TenantRoles).ToList();
            createdTenants.Count.ShouldEqual(5);
            createdTenants.Where(x => x.TenantFullName.StartsWith("Tenant1"))
                .SelectMany(x => x.TenantRoles.Select(x => x.RoleName))
                .Distinct().ShouldEqual(new[] { "RoleAutoAdd" });
            createdTenants.Where(x => x.TenantFullName.StartsWith("Tenant2"))
                .SelectMany(x => x.TenantRoles.Select(x => x.RoleName))
                .Distinct().ShouldEqual(new[] { "RoleAdminAdd", "RoleAutoAdd" });
        }

        [Fact]
        public async Task TestAddTenantsToDatabaseHierarchicalTenantRolesDifferentAtLowerLevelsOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>(dbOptions =>
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions));
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var roleAutoAdd = new RoleToPermissions("RoleAutoAdd", null, $"{(char)2}{(char)3}", RoleTypes.TenantAutoAdd);
            var roleAdminAdd = new RoleToPermissions("RoleAdminAdd", null, $"{(char)1}{(char)3}", RoleTypes.TenantAdminAdd);
            context.AddRange(roleAutoAdd, roleAdminAdd);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var tenantDefinitions = new List<BulkLoadTenantDto>
            {
                new("Tenant1", "RoleAutoAdd", new BulkLoadTenantDto[]
                {
                    new("Tenant1-level1", "RoleAdminAdd", new BulkLoadTenantDto[]
                    {
                        new("Tenant1-level2")
                    })
                })
            };

            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant
            };

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(tenantDefinitions, authOptions);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            context.ChangeTracker.Clear();
            var createdTenants = context.Tenants.Include(x => x.TenantRoles).ToList();
            createdTenants.Count.ShouldEqual(3);
            createdTenants[0].TenantRoles.Select(x => x.RoleName).ShouldEqual(new[] { "RoleAutoAdd" });
            createdTenants[1].TenantRoles.Select(x => x.RoleName).ShouldEqual(new[] { "RoleAdminAdd" });
            createdTenants[2].TenantRoles.Select(x => x.RoleName).ShouldEqual(new[] { "RoleAdminAdd" });
        }

    }
}