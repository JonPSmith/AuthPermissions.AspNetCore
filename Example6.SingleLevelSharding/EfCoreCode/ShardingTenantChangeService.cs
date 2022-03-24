// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Data;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.DataLayer.Classes;
using Example6.SingleLevelSharding.EfCoreClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestSupport.SeedDatabase;

namespace Example6.SingleLevelSharding.EfCoreCode;

/// <summary>
/// This is the <see cref="ITenantChangeService"/> service for a single-level tenant with Sharding turned on.
/// This is different to the non-sharding versions, as we have to create the the instance of the application's
/// DbContext because the connection string relies on the <see cref="Tenant.ConnectionName"/> in the tenant -
/// see <see cref="GetShardingSingleDbContext"/> at the end of this class. This also allows the DataKey to be added
/// which removes the need for using the IgnoreQueryFilters method on any queries
/// </summary>
public class ShardingTenantChangeService : ITenantChangeService
{
    private readonly DbContextOptions<ShardingSingleDbContext> _options;
    private readonly IShardingConnections _connections;
    private readonly ILogger _logger;

    /// <summary>
    /// This allows the tenantId of the deleted tenant to be returned.
    /// This is useful if you want to soft delete the data
    /// </summary>
    public int DeletedTenantId { get; private set; }

    public ShardingTenantChangeService(DbContextOptions<ShardingSingleDbContext> options, 
        IShardingConnections connections, ILogger<ShardingTenantChangeService> logger)
    {
        _options = options;
        _connections = connections;
        _logger = logger;
    }

    /// <summary>
    /// This creates a <see cref="CompanyTenant"/> in the given database
    /// </summary>
    /// <param name="tenant"></param>
    /// <returns></returns>
    public async Task<string> CreateNewTenantAsync(Tenant tenant)
    {
        var context = GetShardingSingleDbContext(tenant.ConnectionName, tenant.GetTenantDataKey());
        if (context == null)
            return $"There is no connection string with the name {tenant.ConnectionName}.";

        if (tenant.HasOwnDb && context.Companies.Any())
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
        var context = GetShardingSingleDbContext(tenant.ConnectionName, tenant.GetTenantDataKey());
        if (context == null)
            return $"There is no connection string with the name {tenant.ConnectionName}.";

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
        var context = GetShardingSingleDbContext(tenant.ConnectionName, tenant.GetTenantDataKey());
        if (context == null)
            return $"There is no connection string with the name {tenant.ConnectionName}.";

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await DeleteTenantData(tenant.GetTenantDataKey(), context);
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
    /// <param name="oldConnectionName"></param>
    /// <param name="oldDataKey"></param>
    /// <param name="updatedTenant"></param>
    /// <returns></returns>
    public async Task<string> MoveToDifferentDatabaseAsync(string oldConnectionName, string oldDataKey, Tenant updatedTenant)
    {
        var oldContext = GetShardingSingleDbContext(oldConnectionName, oldDataKey);
        if (oldContext == null)
            return $"There is no connection string with the name {oldConnectionName}.";

        var newContext = GetShardingSingleDbContext(updatedTenant.ConnectionName, updatedTenant.GetTenantDataKey());
        if (newContext == null)
            return $"There is no connection string with the name {updatedTenant.ConnectionName}.";

        await using var transactionNew = await newContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var invoicesWithLineItems = await oldContext.Invoices.AsNoTracking().Include(x => x.LineItems)
                .ToListAsync();

            //This looks through the entities and resets the primary key to their default value
            var resetter = new DataResetter(newContext);
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


        return null;
    }

    //--------------------------------------------------
    //private methods / classes

    private async Task DeleteTenantData(string dataKey, ShardingSingleDbContext context)
    {
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
    /// <param name="connectionName"></param>
    /// <param name="dataKey"></param>
    /// <returns><see cref="ShardingSingleDbContext"/> or null if connectionName wasn't found in the appsetting file</returns>
    private ShardingSingleDbContext? GetShardingSingleDbContext(string connectionName, string dataKey)
    {
        var connectionString = _connections.GetNamedConnectionString(connectionName);
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