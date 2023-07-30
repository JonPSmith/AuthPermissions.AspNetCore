// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Data;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode.Factories;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
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
        private readonly IDefaultLocalizer _localizeDefault;
        private readonly IAuthPServiceFactory<ITenantChangeService> _tenantChangeServiceFactory;
        private readonly ILogger _logger;

        private readonly TenantTypes _tenantType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="localizeProvider"></param>
        /// <param name="tenantChangeServiceFactory"></param>
        /// <param name="logger"></param>
        public AuthTenantAdminService(AuthPermissionsDbContext context, 
            AuthPermissionsOptions options,
            IAuthPDefaultLocalizer localizeProvider,
            IAuthPServiceFactory<ITenantChangeService> tenantChangeServiceFactory,
            ILogger<AuthTenantAdminService> logger)
        {
            _context = context;
            _options = options;
            _localizeDefault = localizeProvider.DefaultLocalizer;
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
            return _tenantType.IsSingleLevel()
                ? QueryTenants()
                : _context.Tenants.Where(x => !x.Children.Any());
        }

        /// <summary>
        /// This returns a list of all the RoleNames that can be applied to a Tenant
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetRoleNamesForTenantsAsync()
        {
            return await _context.RoleToPermissions
                .Where(x => x.RoleType == RoleTypes.TenantAutoAdd || x.RoleType == RoleTypes.TenantAdminAdd)
                .Select(x => x.RoleName)
                .ToListAsync();
        }

        /// <summary>
        /// This returns a tenant, with TenantRoles and its Parent but no children, that has the given TenantId
        /// </summary>
        /// <param name="tenantId">primary key of the tenant you are looking for</param>
        /// <returns>Status. If successful, then contains the Tenant</returns>
        public async Task<IStatusGeneric<Tenant>> GetTenantViaIdAsync(int tenantId)
        {
            var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);

            var result = await _context.Tenants
                .Include(x => x.Parent)
                .Include(x => x.TenantRoles)
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);
            return result == null 
                ? status.AddErrorString("TenantNotFound".ClassLocalizeKey(this, true),  //common error in this class
                    "Could not find the tenant you were looking for.") 
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
        /// This adds a new, single level Tenant
        /// </summary>
        /// <param name="tenantName">Name of the new single-level tenant (must be unique)</param>
        /// <param name="tenantRoleNames">Optional: List of tenant role names</param>
        /// <param name="hasOwnDb">Needed if sharding: Is true if this tenant has its own database, else false</param>
        /// <param name="databaseInfoName">This is the name of the database information in the shardingsettings file.</param>
        /// <returns>A status containing the <see cref="Tenant"/> class</returns>
        public async Task<IStatusGeneric<Tenant>> AddSingleTenantAsync(string tenantName, List<string> tenantRoleNames = null,
            bool? hasOwnDb = null, string databaseInfoName = null)
        {
            var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);
            status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true), 
                $"Successfully added the new tenant {tenantName}.");

            if (!_tenantType.IsSingleLevel())
                throw new AuthPermissionsException(
                    $"You cannot add a single tenant because the tenant configuration is {_tenantType}");

            var tenantChangeService = _tenantChangeServiceFactory.GetService();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var tenantRolesStatus = await GetRolesWithChecksAsync(tenantRoleNames);
                status.CombineStatuses(tenantRolesStatus);
                var newTenantStatus = Tenant.CreateSingleTenant(tenantName, _localizeDefault, tenantRolesStatus.Result);
                status.SetResult(newTenantStatus.Result);

                if (status.CombineStatuses(newTenantStatus).HasErrors)
                    return status;

                if (_tenantType.IsSharding())
                {
                    if (hasOwnDb == null)
                        status.AddErrorString("HasOwnDbInvalid".ClassMethodLocalizeKey(this, true),
                            $"The '{nameof(hasOwnDb)}' parameter must be set to true or false when sharding is turned on.",
                            nameof(hasOwnDb).CamelToPascal());
                    else
                        status.CombineStatuses(await CheckHasOwnDbIsValidAsync((bool)hasOwnDb, databaseInfoName));

                    if (status.HasErrors)
                        return status;

                    newTenantStatus.Result.UpdateShardingState(
                        databaseInfoName ?? _options.DefaultShardingEntryName,
                        (bool)hasOwnDb);
                }

                _context.Add(newTenantStatus.Result);
                status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

                if (status.HasErrors)
                    return status;

                var errorString = await tenantChangeService.CreateNewTenantAsync(newTenantStatus.Result);
                if (errorString != null)
                    return status.AddErrorString(this.AlreadyLocalized(), errorString); //we assume the tenantChangeService localized its messages

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {e.Message}");
                return status.AddErrorString("ExceptionFail".ClassLocalizeKey(this, true), //same as in Hierarchical
                    "The attempt to create a tenant failed with a system error. Please contact the admin team.");
            }

            return status;
        }

        /// <summary>
        /// This adds a new Hierarchical Tenant, liking it into the parent (which can be null)
        /// </summary>
        /// <param name="tenantName">Name of the new tenant. This will be prefixed with the parent's tenant name to make it unique</param>
        /// <param name="parentTenantId">The primary key of the parent. If 0 then the new tenant is at the top level</param>
        /// <param name="tenantRoleNames">Optional: List of tenant role names</param>
        /// <param name="hasOwnDb">Needed if sharding: Is true if this tenant has its own database, else false</param>
        /// <param name="databaseInfoName">This is the name of the database information in the shardingsettings file.</param>
        /// <returns>A status containing the <see cref="Tenant"/> class</returns>
        public async Task<IStatusGeneric<Tenant>> AddHierarchicalTenantAsync(string tenantName, int parentTenantId,
            List<string> tenantRoleNames = null, bool? hasOwnDb = false, string databaseInfoName = null)
        {
            var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);

            if (!_tenantType.IsHierarchical())
                throw new AuthPermissionsException(
                    $"You must set the {nameof(AuthPermissionsOptions.TenantType)} before you can use tenants");
            if (tenantName.Contains('|'))
                return status.AddErrorString("NameBadChar".ClassLocalizeKey(this, true), //common error in this class
                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order.",
                        nameof(tenantName).CamelToPascal());

            var tenantChangeService = _tenantChangeServiceFactory.GetService();

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                Tenant parentTenant = null;
                if (parentTenantId != 0)
                {
                    //We need to find the parent
                    parentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == parentTenantId);
                    if (parentTenant == null)
                        return status.AddErrorString("ParentNotFound".ClassLocalizeKey(this, true), //common error in this class
                            "Could not find the parent tenant you asked for.");

                    if (!parentTenant.IsHierarchical)
                        throw new AuthPermissionsException(
                            "attempted to add a Hierarchical tenant to a single-level tenant, which isn't allowed");
                }

                var fullTenantName = Tenant.CombineParentNameWithTenantName(tenantName, parentTenant?.TenantFullName);
                status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true), 
                    $"Successfully added the new hierarchical tenant {fullTenantName}.");

                var tenantRolesStatus = await GetRolesWithChecksAsync(tenantRoleNames);
                status.CombineStatuses(tenantRolesStatus);
                var newTenantStatus = Tenant.CreateHierarchicalTenant(fullTenantName, parentTenant, _localizeDefault, tenantRolesStatus.Result);
                status.SetResult(newTenantStatus.Result);
                
                if (status.CombineStatuses(newTenantStatus).HasErrors)
                    return status;

                if (_tenantType.IsSharding())
                {
                    if (parentTenant != null)
                    {
                        //If there is a parent we use its sharding settings
                        //But to make sure the user thinks their values are used we send back errors if they are different 

                        if (hasOwnDb != null && parentTenant.HasOwnDb != hasOwnDb)
                            status.AddErrorFormattedWithParams("InvalidHasOwnDb".ClassMethodLocalizeKey(this, true),
                                new FormattableString[]
                                {
                                    $"The {nameof(hasOwnDb)} parameter doesn't match the parent's ",
                                    $"{nameof(Tenant.HasOwnDb)}. Set the {nameof(hasOwnDb)} ",
                                    $"parameter to null to use the parent's {nameof(Tenant.HasOwnDb)} value."
                                },
                                nameof(hasOwnDb).CamelToPascal());

                        if (databaseInfoName != null &&
                            parentTenant.DatabaseInfoName != databaseInfoName)
                            status.AddErrorFormattedWithParams("InvalidDatabaseInfoName".ClassMethodLocalizeKey(this, true),
                                new FormattableString[]
                                {
                                    $"The {nameof(databaseInfoName)} parameter doesn't match the parent's ",
                                    $"{nameof(Tenant.DatabaseInfoName)}. Set the {nameof(databaseInfoName)} ",
                                    $"parameter to null to use the parent's {nameof(Tenant.DatabaseInfoName)} value."
                                },
                                nameof(databaseInfoName).CamelToPascal());


                        hasOwnDb = parentTenant.HasOwnDb;
                        databaseInfoName = parentTenant.DatabaseInfoName;

                        status.CombineStatuses(await CheckHasOwnDbIsValidAsync((bool)hasOwnDb, databaseInfoName));
                    }
                    else
                    {
                        if (hasOwnDb == null)
                            return status.AddErrorString("HasOwnDbNotSet".ClassMethodLocalizeKey(this, true),
                                $"The {nameof(hasOwnDb)} parameter must be set to true or false if there is no parent and sharding is turned on.",
                                nameof(hasOwnDb).CamelToPascal());

                        status.CombineStatuses(await CheckHasOwnDbIsValidAsync((bool)hasOwnDb, databaseInfoName));
                    }

                    if (status.HasErrors)
                        return status;

                    newTenantStatus.Result.UpdateShardingState(
                        databaseInfoName ?? _options.DefaultShardingEntryName,
                        (bool)hasOwnDb);
                }

                _context.Add(newTenantStatus.Result);
                status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

                if (status.HasErrors)
                    return status;

                var errorString = await tenantChangeService.CreateNewTenantAsync(newTenantStatus.Result);
                if (errorString != null)
                    return status.AddErrorString(this.AlreadyLocalized(), errorString); //we assume the tenantChangeService localized its messages

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {e.Message}");
                return status.AddErrorString("ExceptionFail".ClassLocalizeKey(this, true), //common error in this class
                    "The attempt to create a tenant failed with a system error. Please contact the admin team.");
            }

            return status;
        }

        /// <summary>
        /// This replaces the <see cref="Tenant.TenantRoles"/> in the tenant with <see param="tenantId"/> primary key
        /// </summary>
        /// <param name="tenantId">Primary key of the tenant to change</param>
        /// <param name="newTenantRoleNames">List of RoleName to replace the current tenant's <see cref="Tenant.TenantRoles"/></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateTenantRolesAsync(int tenantId, List<string> newTenantRoleNames)
        {
            if (!_tenantType.IsMultiTenant())
                throw new AuthPermissionsException(
                    $"You must set the {nameof(AuthPermissionsOptions.TenantType)} parameter in the AuthP's options");

            var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);
            status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true), 
                $"Successfully updated the tenant's Roles.");

            var tenant = await _context.Tenants.Include(x => x.TenantRoles)
                .SingleOrDefaultAsync(x => x.TenantId == tenantId);

            if (tenant == null)
                return status.AddErrorString("TenantNotFound".ClassLocalizeKey(this, true), //common error in this class
                    "Could not find the tenant you were looking for.");

            var tenantRolesStatus = await GetRolesWithChecksAsync(newTenantRoleNames);
            if (status.CombineStatuses(tenantRolesStatus).HasErrors)
                return status;

            var updateStatus = tenant.UpdateTenantRoles(tenantRolesStatus.Result, _localizeDefault);
            if (updateStatus.HasErrors)
                return updateStatus;

            return await _context.SaveChangesWithChecksAsync(_localizeDefault);
        }

        /// <summary>
        /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
        /// This also means all the children underneath need to have their full name updated too
        /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="ITenantChangeService"/>
        /// to update the application's tenant data.
        /// </summary>
        /// <param name="tenantId">Primary key of the tenant to change</param>
        /// <param name="newTenantName">This is the new name for this tenant name</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateTenantNameAsync(int tenantId, string newTenantName)
        {
            var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);
            status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true), 
                $"Successfully updated the tenant's name to {newTenantName}.");

            if (string.IsNullOrEmpty(newTenantName))
                return status.AddErrorString("TenantNameEmpty".ClassMethodLocalizeKey(this, true),
                    "The new tenant name was empty.", nameof(newTenantName).CamelToPascal());
            if (newTenantName.Contains('|'))
                return status.AddErrorString("NameBadChar".ClassLocalizeKey(this, true), //common error in this class
                                    "The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order",
                        nameof(newTenantName).CamelToPascal());

            var tenantChangeService = _tenantChangeServiceFactory.GetService();

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var tenant = await _context.Tenants
                    .SingleOrDefaultAsync(x => x.TenantId == tenantId);

                if (tenant == null)
                    return status.AddErrorString("TenantNotFound".ClassLocalizeKey(this, true), //common error in this class
                        "Could not find the tenant you were looking for.");

                if (tenant.IsHierarchical)
                {
                    //We need to load the main tenant and any children and this is the simplest way to do that
                    var tenantsWithChildren = await _context.Tenants
                        .Include(x => x.Parent)
                        .Include(x => x.Children)
                        .Where(x => x.TenantFullName.StartsWith(tenant.TenantFullName))
                        .ToListAsync();

                    var existingTenantWithChildren = tenantsWithChildren
                        .Single(x => x.TenantId == tenantId);

                    existingTenantWithChildren.UpdateTenantName(newTenantName);
                    await tenantChangeService.HierarchicalTenantUpdateNameAsync(tenantsWithChildren);
                }
                else
                {
                    tenant.UpdateTenantName(newTenantName);

                    var errorString = await tenantChangeService.SingleTenantUpdateNameAsync(tenant);
                    if (errorString != null)
                        return status.AddErrorString(this.AlreadyLocalized(), errorString); //we assume the tenantChangeService localized its messages
                }

                status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

                if (status.IsValid)
                    await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {e.Message}");
                return status.AddErrorString("ExceptionFail".ClassLocalizeKey(this, true), //common error in this class
                    "The attempt to create a tenant failed with a system error. Please contact the admin team.");
            }

            return status;
        }

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null). This changes the TenantFullName and the
        /// TenantDataKey of the selected tenant and all of its children
        /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="ResourceLocalize"/>
        /// </summary>
        /// <param name="tenantToMoveId">The primary key of the AuthP tenant to move</param>
        /// <param name="newParentTenantId">Primary key of the new parent, if 0 then you move the tenant to top</param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> MoveHierarchicalTenantToAnotherParentAsync(int tenantToMoveId, int newParentTenantId)
        {
            var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);

            if (!_tenantType.IsHierarchical())
                throw new AuthPermissionsException(
                    $"You cannot add a hierarchical tenant because the tenant configuration is {_tenantType}");

            if (tenantToMoveId == newParentTenantId)
                return status.AddErrorString("NoMoveToSelf".ClassMethodLocalizeKey(this, true),
                    "You cannot move a tenant to itself.", nameof(tenantToMoveId).CamelToPascal());

            var tenantChangeService = _tenantChangeServiceFactory.GetService();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var tenantToMove = await _context.Tenants
                    .SingleOrDefaultAsync(x => x.TenantId == tenantToMoveId);
                var originalName = tenantToMove.TenantFullName;

                var tenantsWithChildren = await _context.Tenants
                    .Include(x => x.Parent)
                    .Include(x => x.Children)
                    .Where(x => x.TenantFullName.StartsWith(tenantToMove.TenantFullName))
                    .ToListAsync();

                var existingTenantWithChildren = tenantsWithChildren
                    .Single(x => x.TenantId == tenantToMoveId);

                Tenant parentTenant = null;
                if (newParentTenantId != 0)
                {
                    //We need to find the parent
                    parentTenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == newParentTenantId);
                    if (parentTenant == null)
                        return status.AddErrorString("ParentNotFound".ClassLocalizeKey(this, true), //common error in this class
                                                    "Could not find the parent tenant you asked for.");

                    if (tenantsWithChildren.Select(x => x.TenantFullName).Contains(parentTenant.TenantFullName))
                        return status.AddErrorString("ParentIsChild".ClassMethodLocalizeKey(this, true), 
                            "You cannot move a tenant one of its children.",
                            nameof(newParentTenantId).CamelToPascal());
                }

                //Now we ask the Tenant entity to do the move on the AuthP's Tenants, and capture each change
                var listOfChanges = new List<(string oldDataKey, Tenant)>();
                existingTenantWithChildren.MoveTenantToNewParent(parentTenant, tuple => listOfChanges.Add(tuple));
                var errorString = await tenantChangeService.MoveHierarchicalTenantDataAsync(listOfChanges);
                if (errorString != null)
                    return status.AddErrorString(this.AlreadyLocalized(), errorString); //we assume the tenantChangeService localized its messages

                status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));
                if (parentTenant != null)
                    status.SetMessageFormatted("Success-ToTenant".ClassMethodLocalizeKey(this, true),
                        $"Successfully moved the tenant originally named '{originalName}' to ",
                        $"the new named '{existingTenantWithChildren.TenantFullName}'.");
                else 
                    status.SetMessageFormatted("Success-ToTop".ClassMethodLocalizeKey(this, true), 
                   $"Successfully moved the tenant originally named '{originalName}' to top level.");

                if (status.IsValid)
                    await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {e.Message}");
                return status.AddErrorString("ExceptionFail".ClassLocalizeKey(this, true), //common error in this class
                    "The attempt to create a tenant failed with a system error. Please contact the admin team.");
            }

            return status;
        }

        /// <summary>
        /// This will delete the tenant (and all its children if the data is hierarchical) and uses the <see cref="ITenantChangeService"/>,
        /// but only if no AuthP user are linked to this tenant (it will return errors listing all the AuthP user that are linked to this tenant
        /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
        /// to delete the application's tenant data.
        /// </summary>
        /// <returns>Status returning the <see cref="ITenantChangeService"/> service, in case you want copy the delete data instead of deleting</returns>
        public async Task<IStatusGeneric<ITenantChangeService>> DeleteTenantAsync(int tenantId)
        {
            var status = new StatusGenericLocalizer<ITenantChangeService>(_localizeDefault);

            var tenantChangeService = _tenantChangeServiceFactory.GetService();
            status.SetResult(tenantChangeService);

            var messages = new List<FormattableString>();
            var messageKey = "Success";

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var tenantToDelete = await _context.Tenants
                    .SingleOrDefaultAsync(x => x.TenantId == tenantId);

                if (tenantToDelete == null)
                    return status.AddErrorString("TenantNotFound".ClassLocalizeKey(this, true), //common error in this class
                        "Could not find the tenant you were looking for.");

                var allTenantIdsAffectedByThisDelete = await _context.Tenants
                    .Include(x => x.Parent)
                    .Include(x => x.Children)
                    .Where(x => x.TenantFullName.StartsWith(tenantToDelete.TenantFullName))
                    .Select(x => x.TenantId)
                    .ToListAsync();

                var usersOfThisTenant = await _context.AuthUsers
                    .Where(x => allTenantIdsAffectedByThisDelete.Contains(x.TenantId ?? 0))
                    .Select(x => x.UserName ?? x.Email)
                    .ToListAsync();

                var tenantOrChildren = allTenantIdsAffectedByThisDelete.Count > 1
                    ? "tenant or its children tenants are"
                    : "tenant is";
                if (usersOfThisTenant.Any())
                    usersOfThisTenant.ForEach(x =>
                        status.AddErrorFormatted("BeingUsedAbort".ClassMethodLocalizeKey(this, true),
                            $"This delete is aborted because this {tenantOrChildren} linked to the user '{x}'."));

                if (status.HasErrors)
                    return status;

                messages.Add( $"Successfully deleted the tenant called '{tenantToDelete.TenantFullName}'");

                if (tenantToDelete.IsHierarchical)
                {
                    //need to delete all the tenants that starts with the main tenant DataKey
                    //We order the tenants with the children first in case a higher level links to a lower level
                    var tenantsInOrder = (await _context.Tenants
                        .Where(x => x.ParentDataKey.StartsWith(tenantToDelete.GetTenantDataKey()))
                        .ToListAsync())
                        .OrderByDescending(x => x.GetTenantDataKey().Count(y => y == '.'))
                        .ToList();
                    //Now we add the parent as the last
                    tenantsInOrder.Add(tenantToDelete);

                    var childError = await tenantChangeService.HierarchicalTenantDeleteAsync(tenantsInOrder);
                    if (childError != null)
                        return status.AddErrorString(this.AlreadyLocalized(), childError); //we assume the tenantChangeService localized its messages

                    if (tenantsInOrder.Count > 0)
                    {
                        _context.RemoveRange(tenantsInOrder);
                        messageKey += "-AndLinked";
                        messages.Add($" and its {tenantsInOrder.Count} linked tenants");
                    }
                }
                else
                {
                    //delete the tenant that the user defines
                    var mainError = await tenantChangeService.SingleTenantDeleteAsync(tenantToDelete);
                    if (mainError != null)
                        return status.AddErrorString(this.AlreadyLocalized(), mainError); //we assume the tenantChangeService localized its messages
                    _context.Remove(tenantToDelete);
                }

                status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

                if (status.IsValid)
                    await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {e.Message}");
                return status.AddErrorString("ExceptionFail".ClassLocalizeKey(this, true), //common error in this class
                    "The attempt to create a tenant failed with a system error. Please contact the admin team.");
            }

            messages.Add($".");
            status.SetMessageFormatted(messageKey.ClassMethodLocalizeKey(this, true), messages.ToArray());
            return status;
        }

        /// <summary>
        /// This is used when sharding is enabled. It updates the tenant's <see cref="Tenant.DatabaseInfoName"/> and
        /// <see cref="Tenant.HasOwnDb"/> and calls the  <see cref="ITenantChangeService"/> <see cref="ITenantChangeService.MoveToDifferentDatabaseAsync"/>
        /// which moves the tenant data to another database and then deletes the the original tenant data.
        /// NOTE: You can change the <see cref="Tenant.HasOwnDb"/> by calling this method with no change to the <see cref="Tenant.DatabaseInfoName"/>.
        /// </summary>
        /// <param name="tenantToMoveId">The primary key of the AuthP tenant to be moved.
        ///     NOTE: If its a hierarchical tenant, then the tenant must be the highest parent.</param>
        /// <param name="hasOwnDb">Says whether the new database will only hold this tenant</param>
        /// <param name="databaseInfoName">This is the name of the database information in the shardingsettings file.</param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> MoveToDifferentDatabaseAsync(int tenantToMoveId, bool hasOwnDb,
            string databaseInfoName)
        {
            var status = new StatusGenericLocalizer<ITenantChangeService>(_localizeDefault);
            status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true),
            $"Successfully moved the tenant to the database defined by the database information with the name '{databaseInfoName}'.");

            if (!_tenantType.IsSharding())
                throw new AuthPermissionsException(
                    "This method can only be called when sharding is turned on.");

            var tenantChangeService = _tenantChangeServiceFactory.GetService();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var tenant = await _context.Tenants
                    .SingleOrDefaultAsync(x => x.TenantId == tenantToMoveId);

                if (tenant == null)
                    return status.AddErrorString("TenantNotFound".ClassLocalizeKey(this, true), //common error in this class
                        "Could not find the tenant you were looking for.");

                if (tenant.IsHierarchical && tenant.ParentDataKey != null)
                    return status.AddErrorString("ChildIsInvalid".ClassMethodLocalizeKey(this, true),
                        "For hierarchical tenants you must provide the top tenant's TenantId, not a child tenant.");

                if (tenant.DatabaseInfoName == databaseInfoName)
                {
                    if (tenant.HasOwnDb == hasOwnDb)
                        return status.AddErrorString("NoChange".ClassMethodLocalizeKey(this, true),
                            "You didn't change any of the sharding parts, so nothing was changed.");

                    status.SetMessageFormatted("SuccessNotMoved".ClassMethodLocalizeKey(this, true), 
                        $"The tenant wasn't moved but its {nameof(Tenant.HasOwnDb)} was changed to {hasOwnDb}.");
                }

                if (status.CombineStatuses(await CheckHasOwnDbIsValidAsync(hasOwnDb, databaseInfoName)).HasErrors)
                    return status;

                var previousDatabaseInfoName = tenant.DatabaseInfoName;
                var previousDataKey = tenant.GetTenantDataKey();
                tenant.UpdateShardingState(databaseInfoName, hasOwnDb);

                if (status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault)).HasErrors)
                    return status;

                if (previousDatabaseInfoName != databaseInfoName)
                {
                    //Just changes the HasNoDb part
                    var mainError = await tenantChangeService
                        .MoveToDifferentDatabaseAsync(previousDatabaseInfoName, previousDataKey, tenant);
                    if (mainError != null)
                        return status.AddErrorString(this.AlreadyLocalized(), mainError); //we assume the tenantChangeService localized its messages
                }

                if (status.IsValid)
                    await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                if (_logger == null)
                    throw;

                _logger.LogError(e, $"Failed to {e.Message}");
                return status.AddErrorString("ExceptionFail".ClassLocalizeKey(this, true), //common error in this class
                    "The attempt to create a tenant failed with a system error. Please contact the admin team.");
            }

            return status;
        }

        //----------------------------------------------------------
        // Common methods

        /// <summary>
        /// This finds the roles with the given names from the AuthP database. Returns errors if not found
        /// NOTE: The Tenant checks that the role's <see cref="RoleToPermissions.RoleType"/> are valid for a tenant
        /// </summary>
        /// <param name="tenantRoleNames">List of role name. Can be null, which means no roles to add</param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric<List<RoleToPermissions>>> GetRolesWithChecksAsync(
            List<string> tenantRoleNames)
        {
            var status = new StatusGenericLocalizer<List<RoleToPermissions>>(_localizeDefault);

            var foundRoles = tenantRoleNames?.Any() == true
                ? await _context.RoleToPermissions
                    .Where(x => tenantRoleNames.Contains(x.RoleName))
                    .Distinct()
                    .ToListAsync()
                : new List<RoleToPermissions>();

            if (foundRoles.Count != (tenantRoleNames?.Count ?? 0))
            {
                foreach (var badRoleName in tenantRoleNames.Where(x => !foundRoles.Select(y => y.RoleName).Contains(x)))
                {
                    status.AddErrorFormatted("RoleNotFound".ClassMethodLocalizeKey(this, true),
                        $"The Role '{badRoleName}' was not found in the lists of Roles.");
                }
            }

            return status.SetResult(foundRoles);
        }

        //----------------------------------------------------------
        // private methods

        /// <summary>
        /// If the hasOwnDb is true, it returns an error if any tenants have the same <see cref="Tenant.DatabaseInfoName"/>
        /// </summary>
        /// <param name="hasOwnDb"></param>
        /// <param name="databaseInfoName"></param>
        /// <returns>status</returns>
        private async Task<IStatusGeneric> CheckHasOwnDbIsValidAsync(bool hasOwnDb, string databaseInfoName)
        {
            var status = new StatusGenericLocalizer(_localizeDefault);
            if (!hasOwnDb)
                return status;

            databaseInfoName ??= _options.DefaultShardingEntryName;

            if (await _context.Tenants.AnyAsync(x => x.DatabaseInfoName == databaseInfoName))
                status.AddErrorFormatted("InvalidDatabase".ClassMethodLocalizeKey(this, true),
                    $"The {nameof(hasOwnDb)} parameter is true, but the sharding database name " ,
                    $"'{databaseInfoName}' already has tenant(s) using that database.");

            return status;
        }
    }
}