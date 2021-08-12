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
            //You have two options on what to do if the DataKey is null
            // (null means no logged in, background service, or user hasn't got an assigned tenant)
            // 1. Set DataKey  to ".", which will means all the multi-data can be seen (good for admin, but watch out for 'no logged in' user)
            // 2. Set DataKey  to a string NOT starting with ".", e.g. "NoAccess". Then no multi-tenant data will be seen
            DataKey = dataKeyFilter?.DataKey ?? "."; 
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
                    entityType.AddStartsWithQueryFilter(this);
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