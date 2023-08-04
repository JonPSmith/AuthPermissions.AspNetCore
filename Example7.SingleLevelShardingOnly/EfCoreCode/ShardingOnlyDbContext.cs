// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
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
        Database.SetConnectionString(shardingDataKeyAndConnect.ConnectionString);
    }

    public DbSet<CompanyTenant> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<LineItem> LineItems { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("invoice");
    }
}