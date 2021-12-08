// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices.Concrete
{
    /// <summary>
    /// Bulk load multiple tenants from a list of <see cref="BulkLoadTenantDto"/>
    /// This works with a single-level tenant scheme and a hierarchical tenant scheme
    /// </summary>
    public class BulkLoadTenantsService : IBulkLoadTenantsService
    {
        private readonly AuthPermissionsDbContext _context;

        /// <summary>
        /// requires access to the AuthPermissionsDbContext
        /// </summary>
        /// <param name="context"></param>
        public BulkLoadTenantsService(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This allows you to add tenants to the database on startup.
        /// It gets the definition of each tenant from the <see cref="BulkLoadTenantDto"/> class
        /// </summary>
        /// <param name="tenantSetupData">If you are using a single layer then each line contains the a tenant name
        /// </param>
        /// <param name="options">The AuthPermissionsOptions to check what type of tenant setting you have</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddTenantsToDatabaseAsync(List<BulkLoadTenantDto> tenantSetupData, AuthPermissionsOptions options)
        {
            var status = new StatusGenericHandler();

            if (tenantSetupData == null || !tenantSetupData.Any())
                return status;

            //Check the options are set
            if (options.TenantType == TenantTypes.NotUsingTenants)
                return status.AddError(
                    $"You must set the options {nameof(AuthPermissionsOptions.TenantType)} to allow tenants to be processed");

            //This takes a COPY of the data because the code stores a tracked tenant in the database
            var tenantsSetupCopy = tenantSetupData.ToList();

            if (options.TenantType == TenantTypes.SingleLevel)
            {
                var duplicateNames = tenantsSetupCopy.Select(x => x.TenantName)
                    .GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
                duplicateNames.ForEach(x => status.AddError($"There is already a Tenant with the name '{x}'"));

                if (status.HasErrors)
                    return status;

                foreach (var tenantDefinition in tenantsSetupCopy)
                {
                    var rolesStatus = GetCheckTenantRoles(tenantDefinition.TenantRolesCommaDelimited,
                        tenantDefinition.TenantName);
                    _context.Add(new Tenant(tenantDefinition.TenantName, rolesStatus.Result));
                }

                return await _context.SaveChangesWithChecksAsync();
            }

            //--------------------------------------------------
            // hierarchical 

            //This uses a transactions because its going to be calling SaveChanges for each layer
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {

                var tenantLevel = tenantsSetupCopy.ToList();
                while (tenantLevel.Any() && status.IsValid)
                {
                    var thisTenantLevel = tenantLevel.ToList();
                    tenantLevel.Clear();

                    foreach (var tenantInfo in thisTenantLevel)
                    {
                        if (tenantInfo.ChildrenTenants != null)
                            tenantLevel.AddRange(tenantInfo.ChildrenTenants);

                        var fullname = Tenant.CombineParentNameWithTenantName(tenantInfo.TenantName,
                            tenantInfo.Parent?.CreatedTenant.TenantFullName);
                        List<RoleToPermissions> tenantRoles = null;
                        if (tenantInfo.TenantRolesCommaDelimited == null)
                            //Use the parents TenantRoles
                            tenantRoles = tenantInfo.Parent?.CreatedTenant.TenantRoles.ToList();
                        else
                        {
                            //Has its 
                            var rolesStatus = GetCheckTenantRoles(tenantInfo.TenantRolesCommaDelimited, fullname);
                            status.CombineStatuses(rolesStatus);
                            if (rolesStatus.IsValid)
                                tenantRoles = rolesStatus.Result;
                        }

                        tenantInfo.CreatedTenant = new Tenant(fullname, tenantInfo.Parent?.CreatedTenant, tenantRoles);
                        _context.Add(tenantInfo.CreatedTenant);
                    }

                    //We have a level done - save them so that the primary key is set
                    if (status.IsValid)
                        status.CombineStatuses(await _context.SaveChangesWithChecksAsync());
                }

                //Done all levels so commit if no errors
                if (status.IsValid)
                    await transaction.CommitAsync();
            }

            return status;
        }

        //-----------------------------------------------------------
        //private parts

        private IStatusGeneric<List<RoleToPermissions>> GetCheckTenantRoles(string tenantRolesCommaDelimited, string fullTenantName)
        {
            var status = new StatusGenericHandler<List<RoleToPermissions>>();

            if (tenantRolesCommaDelimited == null)
                return status.SetResult(null);

            var result = new List<RoleToPermissions>();
            
            var roleNames = tenantRolesCommaDelimited.Split(',').Select(x => x.Trim());
            foreach (var roleName in roleNames)
            {
                var roleToPermission = _context.RoleToPermissions.SingleOrDefault(x => x.RoleName == roleName);
                if (roleToPermission == null)
                    status.AddError($"tenant '{fullTenantName}': the role '{roleName}' was not found in the database");
                else if (roleToPermission.RoleType != RoleTypes.TenantAutoAdd && roleToPermission.RoleType != RoleTypes.TenantAdminAdd)
                    status.AddError($"tenant '{fullTenantName}': the role '{roleName}'s {nameof(RoleToPermissions.RoleType)} must be " +
                                    $"{nameof(RoleTypes.TenantAutoAdd)} or {nameof(RoleTypes.TenantAdminAdd)}");
                else
                    result.Add(roleToPermission);
            }

            return status.SetResult(result.Any() ? result : null);
        }

    }
}