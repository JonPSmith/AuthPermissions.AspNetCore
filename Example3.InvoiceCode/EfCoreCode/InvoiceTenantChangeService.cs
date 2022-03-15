// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.Classes;
using Example3.InvoiceCode.EfCoreClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Example3.InvoiceCode.EfCoreCode
{
    public class InvoiceTenantChangeService : ITenantChangeService
    {
        private readonly InvoicesDbContext _context;
        private readonly ILogger _logger;

        public InvoiceTenantChangeService(InvoicesDbContext context, ILogger<InvoiceTenantChangeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// When a new AuthP Tenant is created, then this method is called. If you have a tenant-type entity in your
        /// application's database, then this allows you to create a new entity for the new tenant.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// NOTE: With hierarchical tenants you cannot be sure that the tenant has, or will have, children
        /// </summary>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
        public async Task<string> CreateNewTenantAsync(string dataKey, int tenantId, string fullTenantName)
        {
            var newCompanyTenant = new CompanyTenant
            {
                DataKey = dataKey,
                AuthPTenantId = tenantId,
                CompanyName = fullTenantName
            };
            _context.Add(newCompanyTenant);
            await _context.SaveChangesAsync();

            return null;
        }

        /// <summary>
        /// This is used with single-level tenant to either
        /// a) delete all the application-side data with the given DataKey, or
        /// b) soft-delete the data.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can provide information of what you have done by adding public parameters to this class.
        ///   The TenantAdmin <see cref="AuthTenantAdminService.DeleteTenantAsync"/> method returns your class on a successful Delete
        /// </summary>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the AuthP part of the delete is rolled back and the return string is shown to the user</returns>

        public async Task<string> SingleTenantDeleteAsync(string dataKey, int tenantId, string fullTenantName)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var deleteSalesSql = $"DELETE FROM invoice.{nameof(InvoicesDbContext.LineItems)} WHERE DataKey = '{dataKey}'";
                await _context.Database.ExecuteSqlRawAsync(deleteSalesSql);
                var deleteStockSql = $"DELETE FROM invoice.{nameof(InvoicesDbContext.Invoices)} WHERE DataKey = '{dataKey}'";
                await _context.Database.ExecuteSqlRawAsync(deleteStockSql);

                var companyTenant = await _context.Set<CompanyTenant>()
                    .IgnoreQueryFilters()
                    .SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
                if (companyTenant != null)
                {
                    _context.Remove(companyTenant);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failure when trying to delete the '{fullTenantName}' tenant.");
                return "There was a system-level problem - see logs for more detail";
            }

            return null;
        }

        /// <summary>
        /// This is called when the name of your Tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// </summary>
        /// <param name="dataKey">The DataKey of the tenant</param>
        /// <param name="tenantId">The TenantId of the tenant</param>
        /// <param name="fullTenantName">The full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the tenant name is rolled back and the return string is shown to the user</returns>
        public async Task<string> HandleUpdateNameAsync(string dataKey, int tenantId, string fullTenantName)
        {
            var companyTenant = await _context.Companies
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
            if (companyTenant != null)
            {
                companyTenant.CompanyName = fullTenantName;
                await _context.SaveChangesAsync();
            }

            return null;
        }

        //Not used
        public Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder)
        {
            //This example is using single level multi-tenant, so this will never be called.

            throw new NotImplementedException();
        }

        //Not used
        public Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> tenantToUpdate)
        {
            //This example is using single level multi-tenant, so this will never be called.

            throw new System.NotImplementedException();
        }
    }
}