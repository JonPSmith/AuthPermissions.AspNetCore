﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public AuthTenantAdminService(AuthPermissionsDbContext context, IAuthPermissionsOptions options)
        {
            _context = context;
            _tenantType = options.TenantType;
        }

        /// <summary>
        /// This simply returns a IQueryable of Tenants
        /// </summary>
        /// <returns>query on the database</returns>
        public IQueryable<Tenant> QueryTenants()
        {
            return _context.Tenants;
        }

        /// <summary>
        /// This adds a new, non-Hierarchical Tenant
        /// </summary>
        /// <param name="tenantName">Name of the new single-level tenant (must be unique)</param>
        /// <returns>A status with any errors found</returns>
        public async Task<IStatusGeneric> AddSingleTenantAsync(string tenantName)
        {
            var status = new StatusGenericHandler { Message = $"Successfully added the new tenant {tenantName}." };

            if (_tenantType != TenantTypes.SingleTenant)
                return status.AddError(
                    $"You cannot add a single tenant because the tenant configuration is {_tenantType}");

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
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}");
            if (thisLevelTenantName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order");

            Tenant parentTenant = null;
            if (parentTenantName != null)
            {
                //We need to find the parent
                parentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantName == parentTenantName);
                if (parentTenant == null)
                    return status.AddError($"Could not find the parent tenant with the name {parentTenantName}");
            }

            var fullTenantName = Tenant.CombineParentNameWithTenantName(thisLevelTenantName, parentTenant?.TenantName);

            _context.Add(new Tenant(fullTenantName, parentTenant));
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = $"Successfully added the new hierarchical tenant {fullTenantName}.";

            return status;
        }

        /// <summary>
        /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
        /// This also means all the children underneath need to have their full name updated too
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <param name="newTenantLevelName"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateTenantNameAsync(string fullTenantName, string newTenantLevelName)
        {
            var status = new StatusGenericHandler();

            if (_tenantType == TenantTypes.NotUsingTenants)
                return status.AddError(
                    $"You haven't configured the TenantType in the configuration.");
            if (string.IsNullOrEmpty(newTenantLevelName))
                return status.AddError("The new name was empty");
            if (newTenantLevelName.Contains('|'))
                return status.AddError(
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order");

            var tenantsWithChildren = await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.TenantName.StartsWith(fullTenantName))
                .ToListAsync();

            var existingTenantWithChildren = tenantsWithChildren
                .SingleOrDefault(x => x.TenantName == fullTenantName);

            if (existingTenantWithChildren == null)
                return status.AddError($"Could not find the tenant with the name {fullTenantName}");

            existingTenantWithChildren.UpdateTenantName(newTenantLevelName);

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());
            status.Message = $"Successfully updated the tenant name the new hierarchical tenant {existingTenantWithChildren.TenantName}.";

            return status;
        }

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null)
        /// This changes the TenantName and the TenantDataKey of the selected tenant and all of its children
        /// </summary>
        /// <param name="fullTenantName">The full name of the tenant to move to another parent </param>
        /// <param name="newParentFullName">he full name of the new parent tenant (can be null, in which case the tenant moved to the top level</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> MoveHierarchicalTenantToAnotherParentAsync(string fullTenantName, string newParentFullName)
        {
            var status = new StatusGenericHandler { };

            if (_tenantType != TenantTypes.HierarchicalTenant)
                return status.AddError(
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}");

            var tenantsWithChildren = await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.Children)
                .Where(x => x.TenantName.StartsWith(fullTenantName))
                .ToListAsync();

            var existingTenantWithChildren = tenantsWithChildren
                .SingleOrDefault(x => x.TenantName == fullTenantName);

            if (existingTenantWithChildren == null)
                return status.AddError($"Could not find the tenant with the name {fullTenantName}");

            Tenant newParentTenant = null;
            if (newParentFullName != null)
            {
                //We need to find the parent
                newParentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantName == newParentFullName);
                if (newParentTenant == null)
                    return status.AddError($"Could not find the parent tenant with the name {newParentFullName}");
            }

            existingTenantWithChildren.MoveTenantToNewParent(newParentTenant);

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = $"Successfully moved the new hierarchical tenant to {existingTenantWithChildren.TenantName}.";

            return status;
        }


    }
}