// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices.Concrete
{
    /// <summary>
    /// Bulk load multiple tenants from a list of <see cref="BulkLoadTenantDto"/>
    /// This works with a single-level tenant scheme and a hierarchical tenant scheme
    /// NOTE: Bulk load doesn't use localization because it doesn't provide to the users
    /// </summary>
    public class BulkLoadTenantsService : IBulkLoadTenantsService
    {
        private readonly AuthPermissionsDbContext _context;
        private Lazy<List<RoleToPermissions>> _lazyRoles;

        /// <summary>
        /// requires access to the AuthPermissionsDbContext
        /// </summary>
        /// <param name="context"></param>
        public BulkLoadTenantsService(AuthPermissionsDbContext context)
        {
            _context = context;
            _lazyRoles = new Lazy<List<RoleToPermissions>>(() =>
                _context.RoleToPermissions.Where(x =>
                        x.RoleType == RoleTypes.TenantAutoAdd || x.RoleType == RoleTypes.TenantAdminAdd)
                    .ToList());
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
            if (!options.TenantType.IsMultiTenant())
                return status.AddError(
                    $"You must set the options {nameof(AuthPermissionsOptions.TenantType)} to allow tenants to be processed");

            //This takes a COPY of the data because the code stores a tracked tenant in the database
            var tenantsSetupCopy = tenantSetupData.ToList();

            if (options.TenantType.IsSingleLevel())
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
                    status.CombineStatuses(rolesStatus);
                    var tenantStatus = Tenant.CreateSingleTenant(tenantDefinition.TenantName, 
                        new StubDefaultLocalizer(), rolesStatus.Result);
                    
                    if (status.CombineStatuses(tenantStatus).IsValid)
                    {
                        if ((options.TenantType & TenantTypes.AddSharding) != 0)
                            tenantStatus.Result.UpdateShardingState(options.DefaultShardingEntryName, false);
                        _context.Add(tenantStatus.Result);
                    }
                }

                if (status.HasErrors)
                    return status;

                return await _context.SaveChangesWithChecksAsync(new StubDefaultLocalizer());
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
                            tenantInfo.Parent?.CreatedTenantFullName);

                        //If this level doesn't have a list of tenant roles, then it uses the parents 
                        tenantInfo.TenantRolesCommaDelimited ??= tenantInfo.Parent?.TenantRolesCommaDelimited;

                        var rolesStatus = GetCheckTenantRoles(tenantInfo.TenantRolesCommaDelimited, fullname);
                        status.CombineStatuses(rolesStatus);
                        if (rolesStatus.HasErrors)
                            continue;

                        var parent = tenantInfo.Parent == null
                            ? null
                            : await _context.Tenants.SingleAsync(x => x.TenantId == tenantInfo.Parent.CreatedTenantId);
                        var newTenantStatus = Tenant.CreateHierarchicalTenant(fullname, parent, 
                            new StubDefaultLocalizer(), rolesStatus.Result);
                        _context.Add(newTenantStatus.Result);

                        if (status.IsValid)
                        {
                            _context.Add(newTenantStatus.Result);
                            if ((options.TenantType & TenantTypes.AddSharding) != 0)
                                newTenantStatus.Result.UpdateShardingState(options.DefaultShardingEntryName, false);
                            status.CombineStatuses(await _context.SaveChangesWithChecksAsync(new StubDefaultLocalizer()));

                            //Now we copy the data so that a child can access to the parent data
                            tenantInfo.CreatedTenantId = newTenantStatus.Result.TenantId;
                            tenantInfo.CreatedTenantFullName = newTenantStatus.Result.TenantFullName;
                        }
                    }
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

            var roleNames = tenantRolesCommaDelimited.Split(',').Select(x => x.Trim())
                .Distinct().ToList();

            //check provided role names are in the database
            var notFoundNames = roleNames
                .Where(x => !_lazyRoles.Value.Select(y => y.RoleName).Contains(x)).ToList();

            foreach (var notFoundName in notFoundNames)
            {
                status.AddError($"Tenant '{fullTenantName}': the role called '{notFoundName}' was not found. Either it is misspent or " +
                                $"the {nameof(RoleToPermissions.RoleType)} must be {nameof(RoleTypes.TenantAutoAdd)} or {nameof(RoleTypes.TenantAdminAdd)}");
            }

            if (status.HasErrors)
                return status;

            return status.SetResult(_lazyRoles.Value.Where(x => roleNames.Contains(x.RoleName)).ToList());
        }

    }
}