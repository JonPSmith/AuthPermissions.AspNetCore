// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Example6.SingleLevelSharding.EfCoreClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Example6.SingleLevelSharding.EfCoreCode;

/// <summary>
/// This is a DBContext that supports sharding 
/// </summary>
public class ShardingSingleDbContext : DbContext, IDataKeyFilterReadOnly
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="shardingDataKeyAndConnect">This uses a service that obtains the DataKey and database connection string
    /// via the claims in the logged in users.</param>
    public ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options,
        IGetShardingDataFromUser shardingDataKeyAndConnect)
        : base(options)
    {
        // The DataKey is null when: no one is logged in, its a background service, or user hasn't got an assigned tenant
        // In these cases its best to set the data key that doesn't match any possible DataKey 
        DataKey = shardingDataKeyAndConnect?.DataKey ?? "stop any user without a DataKey to access the data";

        if (shardingDataKeyAndConnect?.ConnectionString != null)
            //NOTE: If no connection string is provided the DbContext will use the connection it was provided when it was registered
            //If you don't want that to happen, then remove the if above and the connection will be set to null (and fail) 
            Database.SetConnectionString(shardingDataKeyAndConnect.ConnectionString);
    }

    /// <summary>
    /// This is needed for EF Core 9 and above  when building a multi-tenant application.
    /// This allows you to add more than one migration on this database
    /// NOTE: You don't need to add this code if you are building a Sharding-Only type multi-tenant.  
    /// </summary>
    /// <param name="optionsBuilder"></param>
    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => x.Ignore(RelationalEventId.PendingModelChangesWarning));
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<CompanyTenant> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<LineItem> LineItems { get; set; }
    public string DataKey { get; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.MarkWithDataKeyIfNeeded(DataKey);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.MarkWithDataKeyIfNeeded(DataKey);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("invoice");

        //This is want you would manually set up the Query Filter, but there is a easier approach shown after this comment 
        //modelBuilder.Entity<Invoice>().HasQueryFilter(
        //    x => DataKey == MultiTenantExtensions.DataKeyNoQueryFilter || 
        //    x.DataKey == DataKey);
        //modelBuilder.Entity<Invoice>().HasIndex(x => x.DataKey);
        //modelBuilder.Entity<Invoice>().Property(x => DataKey).IsUnicode(false);
        //modelBuilder.Entity<Invoice>().Property(x => DataKey).HasMaxLength(MultiTenantExtensions.DataKeyNoQueryFilter.Length);
        //... and do it again for all the entities in your DbContext
        ;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IDataKeyFilterReadWrite).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSingleTenantShardingQueryFilter(this);
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