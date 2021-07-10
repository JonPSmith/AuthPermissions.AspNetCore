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
        public RetailDbContext(DbContextOptions<RetailDbContext> options, IDataKeyFilter dataKeyFilter)
            : base(options)
        {
            DataKey = dataKeyFilter?.DataKey 
                      ?? "."; //this means AuthP's uses with no tenant can access all the data (useful for admin users)
                              //The alternative is return GUID string if the DataKey is null, which would deny users without a tenant to access the data
        }

        public DbSet<RetailOutlet> RetailOutlets { get; set; }
        public DbSet<ShopStock> ShopStocks { get; set; }
        public DbSet<ShopSale> ShopSales { get; set; }
        public string DataKey { get; }

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