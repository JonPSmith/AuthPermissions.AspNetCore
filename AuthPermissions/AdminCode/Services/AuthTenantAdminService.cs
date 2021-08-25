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
        /// This returns a tenant  with the given TenantId
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns>Status. If successful, then contains the Tenant</returns>
        public async Task<IStatusGeneric<Tenant>> GetTenantViaId(int tenantId)
        {
            var status = new StatusGenericHandler<Tenant>();

            var result = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);
            return result == null 
                ? status.AddError("Could not find the tenant you were looking for.") 
                : status.SetResult(result);
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
        /// <param name="thisLevelTenantName">Name of the new tenant. This will be prefixed with the parent's tenant name to make it unique</param>
        /// <param name="parentTenantName">The name of the parent that this tenant </param>
        /// <returns>A status with any errors found</returns>
        public async Task<IStatusGeneric> AddHierarchicalTenantAsync(string thisLevelTenantName, string parentTenantName)
        {
            var status = new StatusGenericHandler {  };

            if (_tenantType != TenantTypes.HierarchicalTenant)
                return status.AddError(
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}", nameof(thisLevelTenantName).CamelToPascal());
            if (thisLevelTenantName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order",
                        nameof(thisLevelTenantName).CamelToPascal());

            Tenant parentTenant = null;
            if (parentTenantName != null)
            {
                //We need to find the parent
                parentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == parentTenantName);
                if (parentTenant == null)
                    return status.AddError($"Could not find the parent tenant with the name {parentTenantName}",nameof(parentTenantName).CamelToPascal());
            }

            var fullTenantName = Tenant.CombineParentNameWithTenantName(thisLevelTenantName, parentTenant?.TenantFullName);

            _context.Add(new Tenant(fullTenantName, parentTenant));
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = $"Successfully added the new hierarchical tenant {fullTenantName}.";

            return status;
        }

        /// <summary>
        /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
        /// This also means all the children underneath need to have their full name updated too
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="newTenantLevelName"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateTenantNameAsync(int tenantId, string newTenantLevelName)
        {
            var status = new StatusGenericHandler();

            if (_tenantType == TenantTypes.NotUsingTenants)
                return status.AddError(
                    "You haven't configured the TenantType in the configuration.");

            if (string.IsNullOrEmpty(newTenantLevelName))
                return status.AddError("The new name was empty", nameof(newTenantLevelName).CamelToPascal());
            if (newTenantLevelName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order",
                        nameof(newTenantLevelName).CamelToPascal());

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

                existingTenantWithChildren.UpdateTenantName(newTenantLevelName);
            }
            else
            {
                tenant.UpdateTenantName(newTenantLevelName);
            }

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());
            status.Message = $"Successfully updated the tenant name to '{newTenantLevelName}'.";

            return status;
        }

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null)
        /// This changes the TenantFullName and the TenantDataKey of the selected tenant and all of its children
        /// WARNING: If the tenants have data in your database, then you need to change their DataKey using the <see param="getOldNewDataKey"/> action.
        /// </summary>
        /// <param name="fullTenantName">The full name of the tenant to move to another parent </param>
        /// <param name="newParentFullName">he full name of the new parent tenant (can be null, in which case the tenant moved to the top level</param>
        /// <param name="getOldNewDataKey">optional: This action is called at every tenant that is moved.
        /// This allows you to obtains the previous DataKey and the new DataKey of every tenant that was moved so that you can move the data</param>
        /// Providing an action will also stops SaveChangesAsync being called so that you can
        /// <returns>
        /// Returns a status, which has the current AuthPermissionsDbContext, if the <see param="getOldNewDataKey"/> is provided.
        /// This allows you to call the SaveChangesAsync within your 
        /// </returns>
        public async Task<IStatusGeneric<AuthPermissionsDbContext>> MoveHierarchicalTenantToAnotherParentAsync(
            string fullTenantName, string newParentFullName, 
            Action<(string previousDataKey, string newDataKey)> getOldNewDataKey = null)
        {
            if (fullTenantName == null) throw new ArgumentNullException(nameof(fullTenantName));
            if (newParentFullName == null) throw new ArgumentNullException(nameof(newParentFullName));

            fullTenantName = fullTenantName.Trim();
            newParentFullName = newParentFullName.Trim();

            var status = new StatusGenericHandler<AuthPermissionsDbContext> { };

            if (_tenantType != TenantTypes.HierarchicalTenant)
                return status.AddError(
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}");

            if (fullTenantName == newParentFullName)
                return status.AddError("You cannot move a tenant to itself", nameof(newParentFullName).CamelToPascal());

            var tenantsWithChildren = await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.TenantFullName.StartsWith(fullTenantName))
                .ToListAsync();

            var existingTenantWithChildren = tenantsWithChildren
                .SingleOrDefault(x => x.TenantFullName == fullTenantName);

            if (existingTenantWithChildren == null)
                return status.AddError($"Could not find the tenant with the name {fullTenantName}", nameof(fullTenantName).CamelToPascal());

            if ( tenantsWithChildren.Select(x => x.TenantFullName).Contains(newParentFullName))
                return status.AddError("You cannot move a tenant one of its children.", nameof(newParentFullName).CamelToPascal());

            Tenant newParentTenant = null;
            if (newParentFullName != null)
            {
                //We need to find the parent
                newParentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == newParentFullName);
                if (newParentTenant == null)
                    return status.AddError($"Could not find the parent tenant with the name {newParentFullName}", nameof(newParentFullName).CamelToPascal());
            }

            existingTenantWithChildren.MoveTenantToNewParent(newParentTenant, getOldNewDataKey);

            if (getOldNewDataKey != null)
            {
                status.Message = $"WARNING: It is your job to call the SaveChangesAsync to finish the move to tenant {existingTenantWithChildren.TenantFullName}.";
                status.SetResult(_context);
                return status;
            }

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = $"Successfully moved the new hierarchical tenant to {existingTenantWithChildren.TenantFullName}.";

            return status;
        }

        /// <summary>
        /// This will delete the tenant and all its children. It returns a status with the tenant's DataKey
        /// WARNING: This method does NOT delete the data in your application. You need to do that using the DataKey returned in the status Result
        /// </summary>
        /// <param name="fullTenantName">The full name of the tenant to delete, and if hierarchical it deletes all children tenants</param>
        /// <param name="getDeletedTenantData"></param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> DeleteTenantAsync(string fullTenantName,
            Action<(string fullTenantName, string dataKey)> getDeletedTenantData = null)
        {
            var status = new StatusGenericHandler();

            if (_tenantType == TenantTypes.NotUsingTenants)
                return status.AddError(
                    "You haven't configured the TenantType in the configuration.");

            if (string.IsNullOrEmpty(fullTenantName))
                return status.AddError("The new name was empty", nameof(fullTenantName).CamelToPascal());

            var tenantToDelete = await _context.Tenants
                .SingleOrDefaultAsync(x => x.TenantFullName == fullTenantName);

            if (tenantToDelete == null)
                return status.AddError($"Could not find the tenant with the full name of {fullTenantName}.");

            //Get the DataKey to send back
            getDeletedTenantData?.Invoke((tenantToDelete.TenantFullName, tenantToDelete.GetTenantDataKey()));

            var message = $"Successfully deleted the tenant called '{fullTenantName}'";
            if (tenantToDelete.IsHierarchical)
            {
                //need to delete all the tenants that have the 
                var children = await _context.Tenants
                    .Where(x => x.ParentDataKey.StartsWith(tenantToDelete.GetTenantDataKey()))
                    .ToListAsync();

                foreach (var tenant in children)
                {
                    getDeletedTenantData?.Invoke((tenant.TenantFullName, tenant.GetTenantDataKey()));
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