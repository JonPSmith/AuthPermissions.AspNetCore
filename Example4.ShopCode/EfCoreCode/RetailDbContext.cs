// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using Example4.ShopCode.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace Example4.ShopCode.EfCoreCode
{
    public class RetailDbContext : DbContext, IDataKeyFilter
    {
        public string DataKey { get; }

        public RetailDbContext(DbContextOptions<RetailDbContext> options, IDataKeyFilter dataKeyFilter)
            : base(options)
        {
            // The DataKey is null when: no one is logged in, its a background service, or user hasn't got an assigned tenant
            // In these cases its best to set the data key that doesn't match any possible DataKey 
            // Another (very dangerous) option is to set the DataKey  to ".", which will means all the tenant data can be seen by
            //  - users without a tenant (good for admin)
            //  - background services
            //  - and 'no logged in' users! (vey scarey)
            DataKey = dataKeyFilter?.DataKey ?? "stop any user without a DataKey to access the data"; 
        }

        public DbSet<RetailOutlet> RetailOutlets { get; set; }
        public DbSet<ShopStock> ShopStocks { get; set; }
        public DbSet<ShopSale> ShopSales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("retail");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IDataKeyFilter).IsAssignableFrom(entityType.ClrType))
                {
                    entityType.AddHierarchicalTenantReadOnlyQueryFilter(this);
                }

                foreach (var mutableProperty in entityType.GetProperties())
                {
                    if (mutableProperty.ClrType == typeof(decimal))
                    {
                        mutableProperty.SetPrecision(9);
                        mutableProperty.SetScale(2);
                    }
                }
            }
        }
    }
}