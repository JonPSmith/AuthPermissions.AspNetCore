// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StatusGeneric;

namespace AuthPermissions.AdminCode.Services
{
    /// <summary>
    /// This provides CRUD services for tenants
    /// </summary>
    public class AuthTenantAdminService : IAuthTenantAdminService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly AuthPermissionsOptions _options;
        private readonly IAuthPServiceFactory<ITenantChangeService> _tenantChangeServiceFactory;
        private readonly ILogger _logger;

        private readonly TenantTypes _tenantType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="tenantChangeServiceFactory"></param>
        /// <param name="logger"></param>
        public AuthTenantAdminService(AuthPermissionsDbContext context, 
            AuthPermissionsOptions options, 
            IAuthPServiceFactory<ITenantChangeService> tenantChangeServiceFactory,
            ILogger<AuthTenantAdminService> logger)
        {
            _context = context;
            _options = options;
            _tenantChangeServiceFactory = tenantChangeServiceFactory;
            _logger = logger;

            _tenantType = options.TenantType;
        }

        /// <summary>
        /// This simply returns a IQueryable of Tenants
        /// </summary>
        /// <returns>query on the AuthP database</returns>
        public IQueryable<Tenant> QueryTenants()
        {
            return _context.Tenants;
        }

        /// <summary>
        /// This query returns all the end leaf Tenants, which is the bottom of the hierarchy (i.e. no children below it)
        /// </summary>
        /// <returns>query on the AuthP database</returns>
        public IQueryable<Tenant> QueryEndLeafTenants()
        {
            return _tenantType == TenantTypes.SingleLevel
                ? QueryTenants()
                : _context.Tenants.Where(x => !x.Children.Any());
        }

        /// <summary>
        /// This returns a tenant, with its Parent but no children, that has the given TenantId
        /// </summary>
        /// <param name="tenantId">primary key of the tenant you are looking for</param>
        /// <returns>Status. If successful, then contains the Tenant</returns>
        public async Task<IStatusGeneric<Tenant>> GetTenantViaIdAsync(int tenantId)
        {
            var status = new StatusGenericHandler<Tenant>();

            var result = await _context.Tenants
                .Include(x => x.Parent)
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);
            return result == null 
                ? status.AddError("Could not find the tenant you were looking for.") 
                : status.SetResult(result);
        }

        /// <summary>
        /// This returns a list of all the child tenants
        /// </summary>
        /// <param name="tenantId">primary key of the tenant you are looking for</param>
        /// <returns>A list of child tenants for this tenant (can be empty)</returns>
        public async Task<List<Tenant>> GetHierarchicalTenantChildrenViaIdAsync(int tenantId)
        {
            var tenant = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);
            if (tenant == null)
                throw new AuthPermissionsException($"Could not find the tenant with id of {tenantId}");

            if (!tenant.IsHierarchical)
                throw new AuthPermissionsException("This method is only for hierarchical tenants");

            return await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.ParentDataKey.StartsWith(tenant.GetTenantDataKey()))
                .ToListAsync();
        }

        /// <summary>
        /// This adds a new, non-Hierarchical Tenant
        /// </summary>
        /// <param name="tenantName">Name of the new single-level tenant (must be unique)</param>
        /// <returns>A status with any errors found</returns>
        public async Task<IStatusGeneric> AddSingleTenantAsync(string tenantName)
        {
            var status = new StatusGenericHandler { Message = $"Successfully added the new tenant {tenantName}." };

            if (_tenantType != TenantTypes.SingleLevel)
                throw new AuthPermissionsException(
                    $"You cannot add a single tenant  because the tenant configuration is {_tenantType}");

            _context.Add(new Tenant(tenantName));
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            return status;
        }

        /// <summary>
        /// This adds a new Hierarchical Tenant, liking it into the parent (which can be null)
        /// </summary>
        /// <param name="tenantName">Name of the new tenant. This will be prefixed with the parent's tenant name to make it unique</param>
        /// <param name="parentTenantId">The primary key of the parent. If 0 then the new tenant is at the top level</param>
        /// <returns>A status with any errors found</returns>
        public async Task<IStatusGeneric> AddHierarchicalTenantAsync(string tenantName, int parentTenantId)
        {
            var status = new StatusGenericHandler();

            if (_tenantType != TenantTypes.HierarchicalTenant)
                throw new AuthPermissionsException(
                    $"You must set the {nameof(AuthPermissionsOptions.TenantType)} before you can use tenants");
            if (tenantName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order",
                        nameof(tenantName).CamelToPascal());


            Tenant parentTenant = null;
            if (parentTenantId != 0)
            {
                //We need to find the parent
                parentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == parentTenantId);
                if (parentTenant == null)
                    return status.AddError($"Could not find the parent tenant you asked for.");
            }

            var fullTenantName = Tenant.CombineParentNameWithTenantName(tenantName, parentTenant?.TenantFullName);

            _context.Add(new Tenant(fullTenantName, parentTenant));
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = $"Successfully added the new hierarchical tenant {fullTenantName}.";

            return status;
        }

        /// <summary>
        /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
        /// This also means all the children underneath need to have their full name updated too
        /// </summary>
        /// <param name="tenantId">Primary key of the tenant to change</param>
        /// <param name="newTenantName">This is the new name for this tenant name</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateTenantNameAsync(int tenantId, string newTenantName)
        {
            var status = new StatusGenericHandler();

            if (string.IsNullOrEmpty(newTenantName))
                return status.AddError("The new name was empty", nameof(newTenantName).CamelToPascal());
            if (newTenantName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order",
                        nameof(newTenantName).CamelToPascal());

            var tenantChangeService = _tenantChangeServiceFactory.GetService();

            var sqlConnection = GetSqlConnectionWithChecks();

            using var tempAuthContext = CreateAuthPermissionsDbContext(sqlConnection);
            using var appContext = tenantChangeService.GetNewInstanceOfAppContext(sqlConnection);

            using var transaction = await appContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                tempAuthContext.Database.UseTransaction(transaction.GetDbTransaction());
                
                var tenant = await tempAuthContext.Tenants
                    .SingleOrDefaultAsync(x => x.TenantId == tenantId);

                if (tenant == null)
                    return status.AddError("Could not find the tenant you were looking for.");

                if (tenant.IsHierarchical)
                {
                    //We need to load the main tenant and any children and this is the simplest way to do that
                    var tenantsWithChildren = await tempAuthContext.Tenants
                        .Include(x => x.Parent)
                        .Include(x => x.Children)
                        .Where(x => x.TenantFullName.StartsWith(tenant.TenantFullName))
                        .ToListAsync();

                    var existingTenantWithChildren = tenantsWithChildren
                        .Single(x => x.TenantId == tenantId);

                    existingTenantWithChildren.UpdateTenantName(newTenantName);

                    foreach (var tenantToUpdate in tenantsWithChildren)
                    {
                        var errorString = await tenantChangeService.HandleUpdateNameAsync(appContext, tenantToUpdate.GetTenantDataKey(),
                            tenantToUpdate.TenantId, tenantToUpdate.TenantFullName);
                        if (errorString != null)
                            return status.AddError(errorString);
                    }
                }
                else
                {
                    tenant.UpdateTenantName(newTenantName);

                    var errorString = await tenantChangeService.HandleUpdateNameAsync(appContext, tenant.GetTenantDataKey(),
                        tenant.TenantId,
                        tenant.TenantFullName);
                    if (errorString != null)
                        return status.AddError(errorString);
                }

                status.CombineStatuses(await tempAuthContext.SaveChangesWithChecksAsync());

                if (status.IsValid)
                    await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {status.Message}");
                return status.AddError(
                    "The attempt to delete a tenant failed with a system error. Please contact the admin team.");
            }
            status.Message = $"Successfully updated the tenant name to '{newTenantName}'.";

            return status;
        }

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null)
        /// This changes the TenantFullName and the TenantDataKey of the selected tenant and all of its children
        /// WARNING: If the tenants have data in your database, then you need to change their DataKey using the <see param="getOldNewData"/> action.
        /// </summary>
        /// <param name="tenantToMoveId">Primary key of the tenant to move to another parent</param>
        /// <param name="parentTenantId">Primary key of the new parent, if 0 then you move the tenant to </param>
        /// <param name="getOldNewData">This action is called at every tenant that is moved.
        /// This allows you to obtains the previous DataKey, the new DataKey and the fullname of every tenant that was moved
        /// so that you can move the data</param>
        /// <returns>
        /// Returns a status, which has the current AuthPermissionsDbContext, if the <see param="getOldNewData"/> is provided.
        /// This allows you to call the SaveChangesAsync within your 
        /// </returns>
        public async Task<IStatusGeneric<AuthPermissionsDbContext>> MoveHierarchicalTenantToAnotherParentAsync(
            int tenantToMoveId, int parentTenantId, 
            Action<(string previousDataKey, string newDataKey, string newFullName)> getOldNewData)
        {
            var status = new StatusGenericHandler<AuthPermissionsDbContext> { };

            if (_tenantType != TenantTypes.HierarchicalTenant)
                throw new AuthPermissionsException(
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}");

            if (tenantToMoveId == parentTenantId)
                return status.AddError("You cannot move a tenant to itself.", nameof(tenantToMoveId).CamelToPascal());

            var tenantToMove = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantId == tenantToMoveId);
            
            var tenantsWithChildren = await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.TenantFullName.StartsWith(tenantToMove.TenantFullName))
                .ToListAsync();

            var existingTenantWithChildren = tenantsWithChildren
                .Single(x => x.TenantId == tenantToMoveId);

            Tenant parentTenant = null;
            if (parentTenantId != 0)
            {
                //We need to find the parent
                parentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == parentTenantId);
                if (parentTenant == null)
                    return status.AddError("Could not find the parent tenant you asked for.");

                if (tenantsWithChildren.Select(x => x.TenantFullName).Contains(parentTenant.TenantFullName))
                    return status.AddError("You cannot move a tenant one of its children.", nameof(parentTenantId).CamelToPascal());
            }

            existingTenantWithChildren.MoveTenantToNewParent(parentTenant, getOldNewData);

            status.Message = "WARNING: Call SaveChangesAsync on the provided DbContext to update the " +
                             "AuthP database once you have updated the DataKey on the moved data.";
            status.SetResult(_context);
            return status;
        }

        /// <summary>
        /// This will delete the tenant (and all its children if the data is hierarchical) and uses the <see cref="ITenantChangeService"/>
        /// you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService"/> to delete the application's tenant data
        /// </summary>
        /// <returns>Status returning the <see cref="ITenantChangeService"/> service, in case you want copy the delete data instead of deleting</returns>
        public async Task<IStatusGeneric<ITenantChangeService>> DeleteTenantAsync(int tenantId)
        {
            var status = new StatusGenericHandler<ITenantChangeService>();
            string message;

            var tenantChangeService = _tenantChangeServiceFactory.GetService();
            status.SetResult(tenantChangeService);

            var sqlConnection = GetSqlConnectionWithChecks();

            using var tempAuthContext = CreateAuthPermissionsDbContext(sqlConnection);
            using var appContext = tenantChangeService.GetNewInstanceOfAppContext(sqlConnection);

            using var transaction = await appContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                tempAuthContext.Database.UseTransaction(transaction.GetDbTransaction());

                var tenantToDelete = await tempAuthContext.Tenants
                    .SingleOrDefaultAsync(x => x.TenantId == tenantId);

                if (tenantToDelete == null)
                    return status.AddError("Could not find the tenant you were looking for.");

                var allTenantIdsAffectedByThisDelete = await tempAuthContext.Tenants
                    .Include(x => x.Parent)
                    .Include(x => x.Children)
                    .Where(x => x.TenantFullName.StartsWith(tenantToDelete.TenantFullName))
                    .Select(x => x.TenantId)
                    .ToListAsync();

                var usersOfThisTenant = await tempAuthContext.AuthUsers
                    .Where(x => allTenantIdsAffectedByThisDelete.Contains(x.TenantId ?? 0))
                    .Select(x => x.UserName ?? x.Email)
                    .ToListAsync();

                var tenantOrChildren = allTenantIdsAffectedByThisDelete.Count > 1
                    ? "tenant or its children tenants are"
                    : "tenant is";
                if (usersOfThisTenant.Any())
                    usersOfThisTenant.ForEach(x =>
                        status.AddError(
                            $"This delete is aborted because this {tenantOrChildren} linked to the user '{x}'."));

                if (status.HasErrors)
                    return status;

                message = $"Successfully deleted the tenant called '{tenantToDelete.TenantFullName}'";

                if (tenantToDelete.IsHierarchical)
                {
                    //need to delete all the tenants that starts with the main tenant DataKey
                    //We order the tenants with the children first in case a higher level links to a higher level
                    var children = await tempAuthContext.Tenants
                        .Where(x => x.ParentDataKey.StartsWith(tenantToDelete.GetTenantDataKey()))
                        .OrderByDescending(x => x.TenantFullName.Length)
                        .ToListAsync();

                    foreach (var tenant in children)
                    {
                        var childError = await tenantChangeService.HandleTenantDeleteAsync(appContext, tenant.GetTenantDataKey(),
                            tenant.TenantId,
                            tenant.TenantFullName);
                        if (childError != null)
                            return status.AddError(childError);
                    }

                    if (children.Count > 0)
                    {
                        tempAuthContext.RemoveRange(children);
                        message += $" and its {children.Count} linked tenants";
                    }
                }

                //Finally we delete the tenant that the user defines
                var mainError = await tenantChangeService.HandleTenantDeleteAsync(appContext, tenantToDelete.GetTenantDataKey(),
                    tenantToDelete.TenantId,
                    tenantToDelete.TenantFullName);
                if (mainError != null)
                    return status.AddError(mainError);
                tempAuthContext.Remove(tenantToDelete);

                status.CombineStatuses(await tempAuthContext.SaveChangesWithChecksAsync());

                if (status.IsValid)
                    await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {status.Message}");
                return status.AddError(
                    "The attempt to delete a tenant failed with a system error. Please contact the admin team.");
            }

            status.Message = message + ".";
            return status;
        }

        //----------------------------------------------------------
        // private methods

        private SqlConnection GetSqlConnectionWithChecks([CallerMemberName] string callingMethod = "")
        {
            //when unit testing with Sqlite in-memory we use the given context, so no checks
            if (_context.Database.IsSqlite())
                return null;

            var sqlConnection = new SqlConnection(_options.AppConnectionString ?? throw new AuthPermissionsException(
                $"You must set the {nameof(AuthPermissionsOptions.AppConnectionString)} to your application's connection string to use {callingMethod}."));
            if (sqlConnection.ConnectionString != _context.Database.GetConnectionString())
                throw new AuthPermissionsException(
                    $"For the tenant method {callingMethod} to work your application data has to be in the same database as the AuthP data.");

            return sqlConnection;
        }

        //NOTE: when we have multiple database types, then need to pull all creation of a AuthPermissionsDbContext into one place
        private AuthPermissionsDbContext CreateAuthPermissionsDbContext(SqlConnection sqlConnection)
        {
            //when unit testing with Sqlite in-memory we use the given context
            if (_context.Database.IsSqlite())
                return _context;

            var options = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
                .UseSqlServer(sqlConnection, dbOptions =>
                dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));
            EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions.UseExceptionProcessor(options);

            return new AuthPermissionsDbContext(options.Options);
        }
    }
}