// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using Example3.InvoiceCode.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace Example3.InvoiceCode.EfCoreCode
{
    public class InvoicesDbContext : DbContext, IDataKeyFilter
    {
        public string DataKey { get; }

        public InvoicesDbContext(DbContextOptions<InvoicesDbContext> options, IDataKeyFilter dataKeyFilter)
            : base(options)
        {
            // The DataKey is null when: no one is logged in, its a background service, or user hasn't got an assigned tenant
            // In these cases its best to set the data key that doesn't match any possible DataKey 
            DataKey = dataKeyFilter?.DataKey ?? "stop any user without a DataKey to access the data";
        }

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

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IDataKeyFilterReadWrite).IsAssignableFrom(entityType.ClrType))
                {
                    entityType.AddSingleTenantReadWriteQueryFilter(this);
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