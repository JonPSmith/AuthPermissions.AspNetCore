// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using Example4.ShopCode.EfCoreClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Example4.ShopCode.EfCoreCode
{
    public class RetailTenantChangeService : ITenantChangeService
    {
        private readonly RetailDbContext _context;
        private readonly ILogger _logger;

        public IReadOnlyList<int> DeletedTenantIds { get; private set; }

        public RetailTenantChangeService(RetailDbContext context, ILogger<RetailTenantChangeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// When a new AuthP Tenant is created, then this method is called. If you have a tenant-type entity in your
        /// application's database, then this allows you to create a new entity for the new tenant.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// NOTE: With hierarchical tenants you cannot be sure that the tenant has, or will have, children so we always add a retail
        /// </summary>
        /// <param name="tenant">The tenant data used to create a new tenant</param>
        /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
        public async Task<string> CreateNewTenantAsync(Tenant tenant)
        {
            _context.Add(new RetailOutlet(tenant.TenantId, tenant.TenantFullName, tenant.GetTenantDataKey()));
            await _context.SaveChangesAsync();

            return null;
        }

        //not used
        public Task<string> SingleTenantUpdateNameAsync(Tenant tenant)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is called when the name of your Tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// </summary>
        /// <param name="tenantsToUpdate">This contains the tenants to update.</param>
        /// <returns>Returns null if all OK, otherwise the name change is rolled back and the return string is shown to the user</returns>
        public async Task<string> HierarchicalTenantUpdateNameAsync(List<Tenant> tenantsToUpdate)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                foreach (var tenant in tenantsToUpdate)
                {
                    //Higher hierarchical levels don't have data in this example
                    var retailOutletToUpdate =
                        await _context.RetailOutlets
                            .IgnoreQueryFilters().SingleOrDefaultAsync(x => x.AuthPTenantId == tenant.TenantId);

                    if (retailOutletToUpdate != null)
                    {
                        retailOutletToUpdate.UpdateNames(tenant.TenantFullName);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failure when trying to update a hierarchical tenant.");
                return "There was a system-level problem - see logs for more detail";
            }


            return null;
        }

        //Not used
        public Task<string> SingleTenantDeleteAsync(Tenant tenant)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// This is used with hierarchical tenants to either
        /// a) delete all the application-side data with the given DataKey, or
        /// b) soft-delete the data.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can provide information of what you have done by adding public parameters to this class.
        ///   The TenantAdmin <see cref="AuthTenantAdminService.DeleteTenantAsync"/> method returns your class on a successful Delete
        /// </summary>
        /// <param name="tenantsInOrder">The tenants to delete with the children first in case a higher level links to a lower level</param>
        /// <returns>Returns null if all OK, otherwise the AuthP part of the delete is rolled back and the return string is shown to the user</returns>

        public async Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var deletedTenantIds = new List<int>();
                foreach (var tenant in tenantsInOrder)
                {
                    //Higher hierarchical levels don't have data in this example, so it only tries to delete data if there is a RetailOutlet
                    var retailOutletToDelete =
                        await _context.RetailOutlets
                            .IgnoreQueryFilters().SingleOrDefaultAsync(x => x.AuthPTenantId == tenant.TenantId);
                    if (retailOutletToDelete != null)
                    {
                        //yes, its a shop so delete all the stock / sales 
                        var deleteSalesSql =
                            $"DELETE FROM retail.{nameof(RetailDbContext.ShopSales)} WHERE DataKey = '{tenant.GetTenantDataKey()}'";
                        await _context.Database.ExecuteSqlRawAsync(deleteSalesSql);
                        var deleteStockSql =
                            $"DELETE FROM retail.{nameof(RetailDbContext.ShopStocks)} WHERE DataKey = '{tenant.GetTenantDataKey()}'";
                        await _context.Database.ExecuteSqlRawAsync(deleteStockSql);

                        _context.Remove(retailOutletToDelete); //finally delete the RetailOutlet
                        await _context.SaveChangesAsync();
                        deletedTenantIds.Add(tenant.TenantId);
                    }
                }

                await transaction.CommitAsync();
                DeletedTenantIds = deletedTenantIds.AsReadOnly();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failure when trying to delete a hierarchical tenant.");
                return "There was a system-level problem - see logs for more detail";
            }

            

            return null; //null means OK, otherwise the delete is rolled back and the return string is shown to the user
        }


        /// <summary>
        /// This is used with hierarchical tenants, where you move one tenant (and its children) to another tenant
        /// This requires you to change the DataKeys of each application's tenant data, so they link to the new tenant.
        /// Also, if you contain the name of the tenant in your data, then you need to update its new FullName
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can get multiple calls if move a higher level
        /// </summary>
        /// <param name="tenantToUpdate">The data to update each tenant. This starts at the parent and then recursively works down the children</param>
        /// <returns>Returns null if all OK, otherwise AuthP part of the move is rolled back and the return string is shown to the user</returns>
        public async Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, Tenant tenantToMove)> tenantToUpdate)
        {

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                foreach (var tuple in tenantToUpdate)
                {
                    //Higher hierarchical levels don't have data in this example, so it only tries to move data if there is a RetailOutlet
                    var retailOutletMove =
                        await _context.RetailOutlets
                            .IgnoreQueryFilters()
                            .SingleOrDefaultAsync(x => x.AuthPTenantId == tuple.tenantToMove.TenantId);
                    if (retailOutletMove != null)
                    {
                        //yes, its a shop so move all the stock / sales 

                        //This code will update the DataKey of every entity that has the IDataKeyFilterReadOnly interface
                        foreach (var entityType in _context.Model.GetEntityTypes()
                                     .Where(x => typeof(IDataKeyFilterReadOnly).IsAssignableFrom(x.ClrType)))
                        {
                            var updateDataKey = $"UPDATE {entityType.FormSchemaTableFromModel()} " +
                                               $"SET DataKey = '{tuple.tenantToMove.GetTenantDataKey()}' WHERE DataKey = '{tuple.oldDataKey}'";
                            await _context.Database.ExecuteSqlRawAsync(updateDataKey);
                        }

                        retailOutletMove.UpdateNames(tuple.tenantToMove.TenantFullName);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failure when trying to Move a hierarchical tenant.");
                return "There was a system-level problem - see logs for more detail";
            }

            return null;
        }

        public Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName, string oldDataKey,
            Tenant updatedTenant)
        {
            throw new NotImplementedException();
        }
    }
}