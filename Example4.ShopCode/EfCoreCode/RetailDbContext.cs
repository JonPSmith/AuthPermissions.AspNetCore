// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Example4.ShopCode.DataKeyCode;
using Example4.ShopCode.EfCoreClasses;
using Example4.ShopCode.EfCoreClasses.SupportTypes;
using Microsoft.EntityFrameworkCore;

namespace Example4.ShopCode.EfCoreCode
{
    public class RetailDbContext : DbContext, IDataKeyFilter
    {
        public RetailDbContext(DbContextOptions<RetailDbContext> options, IDataKeyFilter dataKeyFilter)
            : base(options)
        {
            DataKey = dataKeyFilter.DataKey;
        }

        public DbSet<RetailOutlet> RetailOutlets { get; set; }
        public DbSet<ShopStock> ShopStocks { get; set; }
        public DbSet<ShopSale> ShopSales { get; set; }
        public string DataKey { get; }

        //I only have to override these two version of SaveChanges, as the other two SaveChanges versions call these
        //public override int SaveChanges(bool acceptAllChangesOnSuccess)
        //{
        //    this.MarkWithDataKeyIfNeeded(DataKey);
        //    return base.SaveChanges(acceptAllChangesOnSuccess);
        //}

        //public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    this.MarkWithDataKeyIfNeeded(DataKey);
        //    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {

                if (typeof(IDataKeyFilter).IsAssignableFrom(entityType.ClrType))
                {
                    entityType.AddUserIdQueryFilter(this);
                }
            }
        }
    }
}