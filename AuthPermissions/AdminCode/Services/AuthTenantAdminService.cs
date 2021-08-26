// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using StatusGeneric;

namespace AuthPermissions.AdminCode.Services
{
    /// <summary>
    /// This provides CRUD services for tenants
    /// </summary>
    public class AuthTenantAdminService : IAuthTenantAdminService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly TenantTypes _tenantType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        public AuthTenantAdminService(AuthPermissionsDbContext context, AuthPermissionsOptions options)
        {
            _context = context;
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
            var status = new StatusGenericHandler<List<Tenant>>();

            var tenant = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);
            if (tenant == null)
                throw new AuthPermissionsException($"Could not find the tenant with id of {tenantId}");

            if (!tenant.IsHierarchical)
                throw new AuthPermissionsException("This method is only for hierarchical tenants");

            return await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.TenantFullName.StartsWith(tenant.TenantFullName) && 
                            x.TenantId != tenantId)
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
                return status.AddError(
                    $"You cannot add a single tenant because the tenant configuration is {_tenantType}", nameof(tenantName).CamelToPascal());

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
            var status = new StatusGenericHandler {  };

            if (_tenantType != TenantTypes.HierarchicalTenant)
                return status.AddError(
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}", nameof(tenantName).CamelToPascal());
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

            if (_tenantType == TenantTypes.NotUsingTenants)
                return status.AddError(
                    "You haven't configured the TenantType in the configuration.");

            if (string.IsNullOrEmpty(newTenantName))
                return status.AddError("The new name was empty", nameof(newTenantName).CamelToPascal());
            if (newTenantName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order",
                        nameof(newTenantName).CamelToPascal());

            var tenant = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);

            if (tenant == null)
                return status.AddError("Could not find the tenant you were looking for.");

            if (tenant.IsHierarchical)
            {
                //We need to load the children and this is the simplest way to do that
                var tenantsWithChildren = await _context.Tenants
                    .Include(x => x.Parent)
                    .Include(x => x.Children)
                    .Where(x => x.TenantFullName.StartsWith(tenant.TenantFullName))
                    .ToListAsync();

                var existingTenantWithChildren = tenantsWithChildren
                    .Single(x => x.TenantId == tenantId);

                existingTenantWithChildren.UpdateTenantName(newTenantName);
            }
            else
            {
                tenant.UpdateTenantName(newTenantName);
            }

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());
            status.Message = $"Successfully updated the tenant name to '{newTenantName}'.";

            return status;
        }

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null)
        /// This changes the TenantFullName and the TenantDataKey of the selected tenant and all of its children
        /// WARNING: If the tenants have data in your database, then you need to change their DataKey using the <see param="getOldNewDataKey"/> action.
        /// </summary>
        /// <param name="tenantToMoveId">Primary key of the tenant to move to another parent</param>
        /// <param name="parentTenantId">Primary key of the new parent, if 0 then you move the tenant to </param>
        /// <param name="getOldNewDataKey">This action is called at every tenant that is moved.
        /// This allows you to obtains the previous DataKey and the new DataKey of every tenant that was moved so that you can move the data</param>
        /// Providing an action will also stops SaveChangesAsync being called so that you can
        /// <returns>
        /// Returns a status, which has the current AuthPermissionsDbContext, if the <see param="getOldNewDataKey"/> is provided.
        /// This allows you to call the SaveChangesAsync within your 
        /// </returns>
        public async Task<IStatusGeneric<AuthPermissionsDbContext>> MoveHierarchicalTenantToAnotherParentAsync(
            int tenantToMoveId, int parentTenantId, 
            Action<(string previousDataKey, string newDataKey)> getOldNewDataKey)
        {
            var status = new StatusGenericHandler<AuthPermissionsDbContext> { };

            if (_tenantType != TenantTypes.HierarchicalTenant)
                return status.AddError(
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

            existingTenantWithChildren.MoveTenantToNewParent(parentTenant, getOldNewDataKey);

            status.Message = "WARNING: Call SaveChangesAsync on the provided DbContext to update the " +
                             "AuthP database once you have updated the DataKey on the moved data.";
            status.SetResult(_context);
            return status;
        }

        /// <summary>
        /// This will delete the tenant (and all its children if the data is hierarchical
        /// WARNING: This method does NOT delete the data in your application. You need to do that using the DataKey returned in the status Result
        /// </summary>
        /// <param name="tenantId">The primary key of the tenant you want to </param>
        /// <param name="getDeletedTenantData">This action is called for each tenant that was deleted to tell you what has been deleted.
        /// You can either use the DataKeys to delete the multi-tenant data, 
        /// or don't delete the data (because it can't be accessed anyway) but show it to the admin user in case you want to re-link it to new tenants</param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> DeleteTenantAsync(int tenantId,
            Action<(string fullTenantName, string dataKey)> getDeletedTenantData)
        {
            var status = new StatusGenericHandler();

            var tenantToDelete = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);

            if (tenantToDelete == null)
                return status.AddError("Could not find the tenant you were looking for.");

            var allTenantIdsAffectedByThisDelete = await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.TenantFullName.StartsWith(tenantToDelete.TenantFullName))
                .Select(x => x.TenantId)
                .ToListAsync();

            var usersOfThisTenant = await _context.AuthUsers.Where(x => allTenantIdsAffectedByThisDelete.Contains(x.TenantId  ?? 0))
                .Select(x => x.UserName ?? x.Email)
                .ToListAsync();

            var tenantOrChildren = allTenantIdsAffectedByThisDelete.Count > 1
                ? "tenant or its children tenants are"
                : "tenant is";
            if (usersOfThisTenant.Any())
                usersOfThisTenant.ForEach(x => status.AddError($"This delete is aborted because this {tenantOrChildren} linked to the user '{x}'."));

            if (status.HasErrors)
                return status;

            //Get the DataKey to send back
            getDeletedTenantData.Invoke((tenantToDelete.TenantFullName, tenantToDelete.GetTenantDataKey()));

            var message = $"Successfully deleted the tenant called '{tenantToDelete.TenantFullName}'";
            if (tenantToDelete.IsHierarchical)
            {
                //need to delete all the tenants that have the 
                var children = await _context.Tenants
                    .Where(x => x.ParentDataKey.StartsWith(tenantToDelete.GetTenantDataKey()))
                    .ToListAsync();

                foreach (var tenant in children)
                {
                    getDeletedTenantData((tenant.TenantFullName, tenant.GetTenantDataKey()));
                }
                if (children.Count > 0)
                {
                    _context.RemoveRange(children);
                    message += $" and its {children.Count} linked tenants";
                }
            }

            //now delete the actual tenant
            _context.Remove(tenantToDelete);

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = message + ".";
            return status;
        }


    }
}