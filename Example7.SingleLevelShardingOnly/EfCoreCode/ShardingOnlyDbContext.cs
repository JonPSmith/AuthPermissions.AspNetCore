// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.GetDataKeyCode;
using Example7.SingleLevelShardingOnly.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace Example7.SingleLevelShardingOnly.EfCoreCode;

/// <summary>
/// This is a DBContext that supports sharding 
/// </summary>
public class ShardingOnlyDbContext : DbContext
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="shardingDataKeyAndConnect">This uses a service that obtains the database connection string
    /// via the claims in the logged in users.</param>
    public ShardingOnlyDbContext(DbContextOptions<ShardingOnlyDbContext> options,
        IGetShardingDataFromUser shardingDataKeyAndConnect)
        : base(options)
    {
        if (shardingDataKeyAndConnect?.ConnectionString != null)
            //Needed to handle the migration of this context
            Database.SetConnectionString(shardingDataKeyAndConnect.ConnectionString);
    }

    public DbSet<CompanyTenant> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<LineItem> LineItems { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("invoice");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
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