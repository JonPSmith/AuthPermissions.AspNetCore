// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Data;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using Example7.SingleLevelShardingOnly.EfCoreClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Example7.SingleLevelShardingOnly.EfCoreCode;

/// <summary>
/// This is the <see cref="ITenantChangeService"/> service for a single-level tenant with Sharding turned on.
/// This is different to the non-sharding versions, as we have to create the the instance of the application's
/// DbContext because the connection string relies on the <see cref="Tenant.DatabaseInfoName"/> in the tenant -
/// see <see cref="GetShardingSingleDbContext"/> at the end of this class. This also allows the DataKey to be added
/// which removes the need for using the IgnoreQueryFilters method on any queries
/// </summary>
public class ShardingOnlyTenantChangeService : ITenantChangeService
{
    private readonly Microsoft.EntityFrameworkCore.DbContextOptions<ShardingOnlyDbContext> _options;
    private readonly IGetSetShardingEntries _shardingService;
    private readonly ILogger _logger;

    /// <summary>
    /// This allows the tenantId of the deleted tenant to be returned.
    /// This is useful if you want to soft delete the data
    /// </summary>
    public int DeletedTenantId { get; private set; }

    public ShardingOnlyTenantChangeService(Microsoft.EntityFrameworkCore.DbContextOptions<ShardingOnlyDbContext> options,
        IGetSetShardingEntries shardingService, ILogger<ShardingOnlyTenantChangeService> logger)
    {
        _options = options;
        _shardingService = shardingService;
        _logger = logger;
    }

    /// <summary>
    /// This creates a <see cref="CompanyTenant"/> in the given database
    /// </summary>
    /// <param name="tenant">The tenant data used to create a new tenant</param>
    /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
    public async Task<string> CreateNewTenantAsync(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName, tenant.GetTenantDataKey());
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        var databaseError = await CheckDatabaseAndPossibleMigrate(context, tenant, true);
        if (databaseError != null) 
            return databaseError;

        var newCompanyTenant = new CompanyTenant
        {
            AuthPTenantId = tenant.TenantId,
            CompanyName = tenant.TenantFullName
        };
        context.Add(newCompanyTenant);
        await context.SaveChangesAsync();

        return null;
    }

    public async Task<string> SingleTenantUpdateNameAsync(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName, tenant.GetTenantDataKey());
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        var companyTenant = await context.Companies
            .SingleOrDefaultAsync(x => x.AuthPTenantId == tenant.TenantId);
        if (companyTenant != null)
        {
            companyTenant.CompanyName = tenant.TenantFullName;
            await context.SaveChangesAsync();
        }

        return null;
    }

    public async Task<string> SingleTenantDeleteAsync(Tenant tenant)
    {
        using var context = GetShardingSingleDbContext(tenant.DatabaseInfoName, tenant.GetTenantDataKey());
        if (context == null)
            return $"There is no connection string with the name {tenant.DatabaseInfoName}.";

        //If the database doesn't exist then log it and return
        if (!await context.Database.CanConnectAsync())
        {
            _logger.LogWarning("DeleteTenantData: asked to remove tenant data / database, but no database found. " +
                               $"Tenant name = {tenant?.TenantFullName ?? "- not available -"}");
            return null;
        }

        //Now we delete it
        DeletedTenantId = tenant.TenantId;
        //The tenant its own database, then you should drop the database, but that depends on what SQL Server provider you use.
        //In this case I can the database because it is on a local SqlServer server.
        try
        {
            await context.Database.EnsureDeletedAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failure when trying to delete the '{tenant.TenantFullName}' tenant.");
            return "There was a system-level problem - see logs for more detail";
        }
        return null;
    }

    public Task<string> HierarchicalTenantUpdateNameAsync(List<Tenant> tenantsToUpdate)
    {
        throw new NotImplementedException();
    }

    public Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder)
    {
        throw new NotImplementedException();
    }

    public Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, Tenant tenantToMove)> tenantToUpdate)
    {
        throw new NotImplementedException();
    }

    public Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName, string oldDataKey, Tenant updatedTenant)
    {
        throw new NotImplementedException();
    }

    //--------------------------------------------------
    //private methods / classes

    /// <summary>
    /// This check is a database is there 
    /// </summary>
    /// <param name="context">The context for the new database</param>
    /// <param name="tenant"></param>
    /// <param name="migrateEvenIfNoDb">If using local SQL server, Migrate will create the database.
    /// That doesn't work on Azure databases</param>
    /// <returns></returns>
    private static async Task<string?> CheckDatabaseAndPossibleMigrate(ShardingOnlyDbContext context, Tenant tenant,
        bool migrateEvenIfNoDb)
    {
        //Thanks to https://stackoverflow.com/questions/33911316/entity-framework-core-how-to-check-if-database-exists
        //There are various options to detect if a database is there - this seems the clearest
        if (!await context.Database.CanConnectAsync())
        {
            //The database doesn't exist
            if (migrateEvenIfNoDb)
                await context.Database.MigrateAsync();
            else
            {
                return $"The database defined by the connection string '{tenant.DatabaseInfoName}' doesn't exist.";
            }
        }
        else if (!await context.Database.GetService<IRelationalDatabaseCreator>().HasTablesAsync())
            //The database exists but needs migrating
            await context.Database.MigrateAsync();

        return null;
    }

    /// <summary>
    /// This create a <see cref="ShardingOnlyDbContext"/> with the correct connection string and DataKey
    /// </summary>
    /// <param name="databaseDataName"></param>
    /// <param name="dataKey"></param>
    /// <returns><see cref="ShardingOnlyDbContext"/> or null if connectionName wasn't found in the appsetting file</returns>
    private ShardingOnlyDbContext? GetShardingSingleDbContext(string databaseDataName, string dataKey)
    {
        var connectionString = _shardingService.FormConnectionString(databaseDataName);
        if (connectionString == null)
            return null;

        return new ShardingOnlyDbContext(_options, new StubGetShardingDataFromUser(connectionString, dataKey));
    }

    private class StubGetShardingDataFromUser : IGetShardingDataFromUser
    {
        public StubGetShardingDataFromUser(string connectionString, string dataKey)
        {
            ConnectionString = connectionString;
            DataKey = dataKey;
        }

        public string DataKey { get; }
        public string ConnectionString { get; }
    }
}