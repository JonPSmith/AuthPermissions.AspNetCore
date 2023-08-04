// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;
using Example4.ShopCode.EfCoreClasses;
using Example4.ShopCode.EfCoreCode;
using Example6.SingleLevelSharding.AppStart;
using Example6.SingleLevelSharding.EfCoreCode;
using Xunit.Extensions.AssertExtensions;

namespace Test.TestHelpers
{
    public static class AuthPSetupHelpers
    {
        /// <summary>
        /// Use this to create a AuthUser in your tests
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        /// <param name="roles"></param>
        /// <param name="userTenant"></param>
        /// <returns></returns>
        public static AuthUser CreateTestAuthUserOk(string userId, string email, string userName,
            List<RoleToPermissions> roles = null, Tenant userTenant = null)
        {
            var status = AuthUser.CreateAuthUser(userId, email, userName, roles ?? new List<RoleToPermissions>(),
                "en".SetupAuthPLoggingLocalizer().DefaultLocalizer, userTenant);
            status.IfErrorsTurnToException();
            return status.Result;
        }

        /// <summary>
        /// Use this to create a SingleTenant Tenant in your tests
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <param name="tenantRoles"></param>
        /// <returns></returns>
        public static Tenant CreateTestSingleTenantOk(string fullTenantName, List<RoleToPermissions> tenantRoles = null)
        {
            var status = Tenant.CreateSingleTenant(fullTenantName, 
                "en".SetupAuthPLoggingLocalizer().DefaultLocalizer, tenantRoles);
            status.IfErrorsTurnToException();
            return status.Result;
        }

        /// <summary>
        /// Use this to create a Hierarchical Tenant in your tests
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <param name="tenantRoles"></param>
        /// <returns></returns>
        public static Tenant CreateTestHierarchicalTenantOk(string fullTenantName, Tenant parent, List<RoleToPermissions> tenantRoles = null)
        {
            var status = Tenant.CreateHierarchicalTenant(fullTenantName, parent,
                "en".SetupAuthPLoggingLocalizer().DefaultLocalizer, tenantRoles);
            status.IfErrorsTurnToException();
            return status.Result;
        }

        /// <summary>
        /// Use this to create a single sharding Tenant in your tests
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <param name="databaseInfoName"></param>
        /// <param name="hasOwnDb"></param>
        /// <returns></returns>
        public static Tenant CreateSingleShardingTenant(this string fullTenantName, string databaseInfoName, bool hasOwnDb)
        {
            var status = Tenant.CreateSingleTenant(fullTenantName,
                "en".SetupAuthPLoggingLocalizer().DefaultLocalizer);
            status.IfErrorsTurnToException();
            status.Result.UpdateShardingState(databaseInfoName, hasOwnDb);
            return status.Result;
        }

        /// <summary>
        /// Use this to create a hierarchical sharding Tenant in your tests
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <param name="databaseInfoName"></param>
        /// <param name="hasOwnDb"></param>
        /// <param name="parentTenant"></param>
        /// <returns></returns>
        public static Tenant CreateHierarchicalShardingTenant(this string fullTenantName, string databaseInfoName,
            bool hasOwnDb, Tenant? parentTenant = null)
        {
            var status = Tenant.CreateHierarchicalTenant(fullTenantName, parentTenant,
                "en".SetupAuthPLoggingLocalizer().DefaultLocalizer);
            status.IfErrorsTurnToException();
            status.Result.UpdateShardingState(databaseInfoName, hasOwnDb);
            return status.Result;
        }

        public static AuthPJwtConfiguration CreateTestJwtSetupData(TimeSpan expiresIn = default)
        {

            var data = new AuthPJwtConfiguration
            {
                Issuer = "issuer",
                Audience = "audience",
                SigningKey = "long-key-with-lots-of-data-in-it",
                TokenExpires = expiresIn == default ? new TimeSpan(0, 0, 50) : expiresIn,
                RefreshTokenExpires = expiresIn == default ? new TimeSpan(0, 0, 50) : expiresIn,
            };

            return data;
        }

        public static readonly List<BulkLoadRolesDto> TestRolesDefinition123 = new List<BulkLoadRolesDto>()
        {
            new("Role1", null, "One"),
            new("Role2", "my description", "Two"),
            new("Role3", null, "Three"),
        };

        public static async Task SetupRolesInDbAsync(this AuthPermissionsDbContext context)
        {
            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(TestEnum);
            var service = new BulkLoadRolesService(context, authOptions);
            var status = await service.AddRolesToDatabaseAsync(TestRolesDefinition123);
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();
        }

        public static void AddOneUserWithRolesAndOptionalTenant(this AuthPermissionsDbContext context, 
            string email = "User1@g.com", string tenantName = null)
        {
            var rolePer1 = new RoleToPermissions("Role1", null, $"{(char)1}{(char)3}");
            var rolePer2 = new RoleToPermissions("Role2", null, $"{(char)2}{(char)3}");
            context.AddRange(rolePer1, rolePer2);
            var tenant = tenantName != null
                ? context.Tenants.Single(x => x.TenantFullName == tenantName)
                : null;
            var user = CreateTestAuthUserOk("User1", email, null, 
                new List<RoleToPermissions>() { rolePer1 }, tenant);
            context.Add(user);
            context.SaveChanges();
        }

        /// <summary>
        /// This adds AuthUser with an ever increasing number of roles
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userIdCommaDelimited"></param>
        public static void AddMultipleUsersWithRolesInDb(this AuthPermissionsDbContext context, string userIdCommaDelimited = "User1,User2,User3")
        {
            var rolesInDb = context.RoleToPermissions.OrderBy(x => x.RoleName).ToList();
            var userIds = userIdCommaDelimited.Split(',');
            for (int i = 0; i < userIds.Length; i++)
            {
                var user = CreateTestAuthUserOk(userIds[i], $"{userIds[i]}@gmail.com", 
                    $"first last {i}", rolesInDb.Take(i+1).ToList());
                context.Add(user);
            }
            context.SaveChanges();
        }

        public static List<int> SetupSingleTenantsInDb(this AuthPermissionsDbContext context, InvoicesDbContext invoiceContext = null)
        {
            var tenants = new []
            {
                CreateTestSingleTenantOk("Tenant1"),
                CreateTestSingleTenantOk("Tenant2"),
                CreateTestSingleTenantOk("Tenant3"),
            };

            context.AddRange(tenants);
            context.SaveChanges();

            if (invoiceContext != null)
            {
                foreach (var tenant in tenants)
                {
                    var company = new CompanyTenant
                    {
                        AuthPTenantId = tenant.TenantId,
                        CompanyName = tenant.TenantFullName,
                        DataKey = tenant.GetTenantDataKey(),
                    };
                    invoiceContext.Add(company);
                }

                invoiceContext.SaveChanges();
            }

            return tenants.Select(x => x.TenantId).ToList();
        }

        public static async Task<List<int>> SetupSingleShardingTenantsInDbAsync(this AuthPermissionsDbContext context,
            ShardingSingleDbContext appContext = null)
        {
            var tenants = new List<Tenant>
            {
                CreateTestSingleTenantOk("Tenant1"),
                CreateTestSingleTenantOk("Tenant2"),
                CreateTestSingleTenantOk("Tenant3"),
            };

            tenants.ForEach(x => x.UpdateShardingState("Default Database", false));

            context.AddRange(tenants);
            context.SaveChanges();

            if (appContext != null)
            {
                var seeder = new SeedShardingDbContext(appContext);
                await seeder.SeedInvoicesForAllTenantsAsync(tenants);
            }

            return tenants.Select(x => x.TenantId).ToList();
        }

        public static List<BulkLoadTenantDto> GetSingleTenant123()
        {
            return new List<BulkLoadTenantDto>
            {
                new("Tenant1"),
                new("Tenant2"),
                new("Tenant3"),
            };
        }

        public static List<BulkLoadTenantDto> GetHierarchicalDefinitionCompany()
        {
            return new List<BulkLoadTenantDto>()
            {
                new("Company", null, new BulkLoadTenantDto[]
                {
                    new ("West Coast", null, new BulkLoadTenantDto[]
                    {
                        new ("SanFran", null, new BulkLoadTenantDto[]
                        {
                            new ("Shop1"),
                            new ("Shop2")
                        })
                    }),
                    new ("East Coast", null, new BulkLoadTenantDto[]
                    {
                        new ("New York", null, new BulkLoadTenantDto[]
                        {
                            new ("Shop3"),
                            new ("Shop4")
                        })
                    })
                })
            };
        }
            
        public static async Task<List<int>> BulkLoadHierarchicalTenantInDbAsync(this AuthPermissionsDbContext context,
            RetailDbContext retailContext = null)
        {
            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions {TenantType = TenantTypes.HierarchicalTenant};

            (await service.AddTenantsToDatabaseAsync(GetHierarchicalDefinitionCompany(), authOptions)).IsValid.ShouldBeTrue();
            if (retailContext != null)
            {
                //We add
                foreach (var tenant in context.Tenants)
                {
                    retailContext.Add(new RetailOutlet(tenant.TenantId, tenant.TenantFullName,
                        tenant.GetTenantDataKey()));
                }

                retailContext.SaveChanges();
            }

            return context.Tenants.Select(x => x.TenantId).OrderBy(x => x).ToList();
        }

        public static async Task<List<int>> BulkLoadHierarchicalTenantShardingAsync(this AuthPermissionsDbContext context)
        {
            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant };

            (await service.AddTenantsToDatabaseAsync(GetHierarchicalDefinitionCompany(), authOptions)).IsValid.ShouldBeTrue();
            context.Tenants.ToList().ForEach(x => x.UpdateShardingState("Default Database", false));
            await context.SaveChangesAsync();

            return context.Tenants.Select(x => x.TenantId).OrderBy(x => x).ToList();
        }

        public static List<BulkLoadUserWithRolesTenant> TestUserDefineWithUserId(string user2Roles = "Role1,Role2")
        {
            return new List<BulkLoadUserWithRolesTenant>
            {
                new ("User1", null, "Role1", userId: "1"),
                new ("User2", null, user2Roles, userId: "2"),
                new ("User3", null, "Role1,Role3", userId: "3"),
            };
        }

        public static List<BulkLoadUserWithRolesTenant> TestUserDefineNoUserId(string user2Id = "User2")
        {
            return new List<BulkLoadUserWithRolesTenant>
            {
                new ("User1", null, "Role1", userId: "1"),
                new ("User2", null, "Role1,Role2", userId: user2Id),
                new ("User3", null, "Role1,Role3", userId: "3"),
            };
        }        
        
        public static List<BulkLoadUserWithRolesTenant> TestUserDefineWithSuperUser(string user2Id = "User2")
        {
            return new List<BulkLoadUserWithRolesTenant>
            {
                new ("User1", null, "Role1", userId: "1"),
                new ("Super@g1.com",null,  "Role1,Role2", userId: null),
                new ("User3", null, "Role1,Role3", userId: "3"),
            };
        }

        public static List<BulkLoadUserWithRolesTenant> TestUserDefineWithTenants(string secondTenant = "Tenant2")
        {
            return new List<BulkLoadUserWithRolesTenant>
            {
                new ("User1", null, "Role1", userId: "1", uniqueUserName: null, tenantNameForDataKey: "Tenant1"),
                new ("User2", null, "Role1,Role2", userId: "2", uniqueUserName: null, tenantNameForDataKey: secondTenant),
                new ("User3", null, "Role1,Role3", userId: "3", uniqueUserName: null, tenantNameForDataKey: "Tenant3")
            };
        }
    }
}