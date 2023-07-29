// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Data;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using Example6.SingleLevelSharding.EfCoreClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TestSupport.SeedDatabase;

namespace Example6.SingleLevelSharding.EfCoreCode;

/// <summary>
/// This is the <see cref="ITenantChangeService"/> service for a single-level tenant with Sharding turned on.
/// This is different to the non-sharding versions, as we have to create the the instance of the application's
/// DbContext because the connection string relies on the <see cref="Tenant.DatabaseInfoName"/> in the tenant -
/// see <see cref="GetShardingSingleDbContext"/> at the end of this class. This also allows the DataKey to be added
/// which removes the need for using the IgnoreQueryFilters method on any queries
/// </summary>
public class ShardingTenantChangeService : ITenantChangeService
{
    private readonly DbContextOptions<ShardingSingleDbContext> _options;
    private readonly IGetSetShardingEntries _shardingService;
    private readonly ILogger _logger;

    /// <summary>
    /// This allows the tenantId of the deleted tenant to be returned.
    /// This is useful if you want to soft delete the data
    /// </summary>
    public int DeletedTenantId { get; private set; }

    public ShardingTenantChangeService(DbContextOptions<ShardingSingleDbContext> options,
        IGetSetShardingEntries shardingService, ILogger<ShardingTenantChangeService> logger)
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

        if (tenant.HasOwnDb && context.Companies.IgnoreQueryFilters().Any())
            return
                $"The tenant's {nameof(Tenant.HasOwnDb)} property is true, but the database contains existing companies";

        var newCompanyTenant = new CompanyTenant
        {
            DataKey = tenant.GetTenantDataKey(),
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

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await DeleteTenantData(tenant.GetTenantDataKey(), context, tenant);
            DeletedTenantId = tenant.TenantId;

            await transaction.CommitAsync();
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

    /// <summary>
    /// This method can be quite complicated. It has to
    /// 1. Copy the data from the previous database into the new database
    /// 2. Delete the old data
    /// These two steps have to be done within a transaction, so that a failure to delete the old data will roll back the copy.
    /// </summary>
    /// <param name="oldDatabaseInfoName"></param>
    /// <param name="oldDataKey"></param>
    /// <param name="updatedTenant"></param>
    /// <returns></returns>
    public async Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName, string oldDataKey, Tenant updatedTenant)
    {
        //NOTE: The oldContext and newContext have the correct DataKey so you don't have to use IgnoreQueryFilters.
        var oldContext = GetShardingSingleDbContext(oldDatabaseInfoName, oldDataKey);
        if (oldContext == null)
            return $"There is no connection string with the name {oldDatabaseInfoName}.";

        var newContext = GetShardingSingleDbContext(updatedTenant.DatabaseInfoName, updatedTenant.GetTenantDataKey());
        if (newContext == null)
            return $"There is no connection string with the name {updatedTenant.DatabaseInfoName}.";

        var databaseError = await CheckDatabaseAndPossibleMigrate(newContext, updatedTenant, true);
        if (databaseError != null)
            return databaseError;

        await using var transactionNew = await newContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var invoicesWithLineItems = await oldContext.Invoices.AsNoTracking().Include(x => x.LineItems)
                .ToListAsync();

            
            //NOTE: writing the entities to the database will set the DataKey on a non-sharding tenant,
            //but if its a sharding tenant then the DataKey won't be changed, BUT if you want the DataKey cleared out see the RetailTenantChangeService.MoveHierarchicalTenantDataAsync to manually set the DataKey
            var resetter = new DataResetter(newContext);
            //This resets the primary / foreign keys to their default value ready to write into the new database
            //This method comes from my EfCore.TestSupport library as was used to store data and add it back.
            //see the extract part documentation vai https://github.com/JonPSmith/EfCore.TestSupport/wiki/Seed-from-Production-feature
            resetter.ResetKeysEntityAndRelationships(invoicesWithLineItems);

            newContext.AddRange(invoicesWithLineItems);

            var companyTenant = await oldContext.Companies.AsNoTracking().SingleOrDefaultAsync();
            if (companyTenant != null)
            {
                companyTenant.CompanyTenantId = default;
                newContext.Add(companyTenant);
            }

            await newContext.SaveChangesAsync();

            //Now we try to delete the old data
            await using var transactionOld = await oldContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await DeleteTenantData(oldDataKey, oldContext);

                await transactionOld.CommitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failure when trying to delete the original tenant data after the copy over.");
                return "There was a system-level problem - see logs for more detail";
            }

            await transactionNew.CommitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failure when trying to copy the tenant data to the new database.");
            return "There was a system-level problem - see logs for more detail";
        }

        return null;
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
    private static async Task<string?> CheckDatabaseAndPossibleMigrate(ShardingSingleDbContext context, Tenant tenant,
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

    private async Task DeleteTenantData(string dataKey, ShardingSingleDbContext context, Tenant? tenant = null)
    {
        if (tenant?.HasOwnDb == true)
        {
            //The tenant its own database, then you should drop the database, but that depends on what SQL Server provider you use.
            //In this case I can the database because it is on a local SqlServer server.
            await context.Database.EnsureDeletedAsync();
            return;
        }

        //else we remove all the data with the DataKey of the tenant
        var deleteSalesSql = $"DELETE FROM invoice.{nameof(ShardingSingleDbContext.LineItems)} WHERE DataKey = '{dataKey}'";
        await context.Database.ExecuteSqlRawAsync(deleteSalesSql);
        var deleteStockSql = $"DELETE FROM invoice.{nameof(ShardingSingleDbContext.Invoices)} WHERE DataKey = '{dataKey}'";
        await context.Database.ExecuteSqlRawAsync(deleteStockSql);

        var companyTenant = await context.Companies.SingleOrDefaultAsync();
        if (companyTenant != null)
        {
            context.Remove(companyTenant);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// This create a <see cref="ShardingSingleDbContext"/> with the correct connection string and DataKey
    /// </summary>
    /// <param name="databaseDataName"></param>
    /// <param name="dataKey"></param>
    /// <returns><see cref="ShardingSingleDbContext"/> or null if connectionName wasn't found in the appsetting file</returns>
    private ShardingSingleDbContext? GetShardingSingleDbContext(string databaseDataName, string dataKey)
    {
        var connectionString = _shardingService.FormConnectionString(databaseDataName);
        if (connectionString == null)
            return null;

        return new ShardingSingleDbContext(_options, new StubGetShardingDataFromUser(connectionString, dataKey));
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