// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using Example3.InvoiceCode.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace Example3.InvoiceCode.EfCoreCode
{
    public class InvoicesDbContext : DbContext, IDataKeyFilterReadOnly
    {
        public string DataKey { get; }

        public InvoicesDbContext(DbContextOptions<InvoicesDbContext> options, IGetDataKeyFromUser dataKeyFilter)
            : base(options)
        {
            // The DataKey is null when: no one is logged in, its a background service, or user hasn't got an assigned tenant
            // In these cases its best to set the data key that doesn't match any possible DataKey 
            DataKey = dataKeyFilter?.DataKey ?? "stop any user without a DataKey to access the data";
        }

        public DbSet<CompanyTenant> Companies { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<LineItem> LineItems { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.MarkWithDataKeyIfNeeded(DataKey);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            this.MarkWithDataKeyIfNeeded(DataKey);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("invoice");

            // You could manually set up the Query Filter, but there is a easier approach
            //modelBuilder.Entity<Invoice>().HasQueryFilter(x => x.DataKey == DataKey);
            //modelBuilder.Entity<LineItem>().HasQueryFilter(x => x.DataKey == DataKey);
            //modelBuilder.Entity<CompanyTenant>().HasQueryFilter(x => x.DataKey == DataKey);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IDataKeyFilterReadWrite).IsAssignableFrom(entityType.ClrType))
                {
                    entityType.AddSingleTenantReadWriteQueryFilter(this);
                }
                else
                {
                    throw new Exception(
                        $"You haven't added the {nameof(IDataKeyFilterReadWrite)} to the entity {entityType.ClrType.Name}");
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