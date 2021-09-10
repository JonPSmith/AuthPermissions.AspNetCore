// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthPermissions.AdminCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using Example4.ShopCode.EfCoreClasses;
using Example4.ShopCode.EfCoreCode;

namespace Test.TestHelpers
{
    public static class RetailSetupHelpers
    {
        public static void SetupSingleRetailAndStock(this RetailDbContext context, params string[] tenantNames)
        {
            if (!tenantNames.Any())
                tenantNames = new string[] { "Tenant1", "Tenant2", "Tenant3" };

            int i = 0;
            foreach (var tenantName in tenantNames)
            {
                i++;
                var retail = new RetailOutlet(new StubAuthTenant($".{i}", i, tenantName));
                var stock = new ShopStock("stuff", 123, 5, retail);
                context.AddRange(retail, stock);
            }

            context.SaveChanges();
        }

        public static void SetupHierarchicalRetailAndStock(this RetailDbContext context, AuthPermissionsDbContext authContext)
        {
            var leafTenants = authContext.Tenants.Where(x => !x.Children.Any()).ToList();

            foreach (var tenant in leafTenants)
            {
                var retail = new RetailOutlet(tenant);
                var stock = new ShopStock("stuff", 123, 5, retail);
                var sale = ShopSale.CreateSellAndUpdateStock(1, stock, "stuff").Result;
                context.AddRange(retail, stock, sale);
            }

            context.SaveChanges();
        }

        private class StubAuthTenant : ITenantPartsToExport
        {
            private readonly string _dataKey;

            public StubAuthTenant(string dataKey, int tenantId, string tenantFullName)
            {
                _dataKey = dataKey;
                TenantId = tenantId;
                TenantFullName = tenantFullName;
            }

            public int TenantId { get; }
            public string TenantFullName { get; }
            public string GetTenantDataKey() => _dataKey;
        }
    }
}