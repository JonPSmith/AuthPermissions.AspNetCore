// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using LocalizeMessagesAndErrors.UnitTestingCode;
using StatusGeneric;
using Test.TestHelpers;

namespace Test.StubClasses;

/// <summary>
/// NOTE: the only method that works is the GetTenantViaIdAsync
/// </summary>
public class StubAuthTenantAdminService : IAuthTenantAdminService
{
    public string CalledMethodName { get; private set; }

    private readonly Tenant[] _tenants;

    public StubAuthTenantAdminService(params Tenant[] tenants)
    {
        _tenants = tenants;
    }

    /// <summary>
    /// This simply returns a IQueryable of Tenants
    /// </summary>
    /// <returns>query on the AuthP database</returns>
    public IQueryable<Tenant> QueryTenants()
    {
        return _tenants.AsQueryable();
    }

    /// <summary>
    /// This query returns all the end leaf Tenants, which is the bottom of the hierarchy (i.e. no children below it)
    /// </summary>
    /// <returns>query on the AuthP database</returns>
    public IQueryable<Tenant> QueryEndLeafTenants()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This returns a list of all the RoleNames that can be applied to a Tenant
    /// </summary>
    /// <returns></returns>
    public Task<List<string>> GetRoleNamesForTenantsAsync()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This returns a tenant, with TenantRoles and its Parent but no children, that has the given TenantId
    /// </summary>
    /// <param name="tenantId">primary key of the tenant you are looking for</param>
    /// <returns>Status. If successful, then contains the Tenant</returns>
    public Task<IStatusGeneric<Tenant>> GetTenantViaIdAsync(int tenantId)
    {
        var status = new StatusGenericHandler<Tenant>();
        var foundTenant = _tenants.SingleOrDefault(x => x.TenantId == tenantId);
        status.SetResult(foundTenant);
        if (status.Result == null)
            status.AddError("No tenant found");
        return Task.FromResult<IStatusGeneric<Tenant>>(status);
    }

    /// <summary>
    /// This returns a list of all the child tenants
    /// </summary>
    /// <param name="tenantId">primary key of the tenant you are looking for</param>
    /// <returns>A list of child tenants for this tenant (can be empty)</returns>
    public Task<List<Tenant>> GetHierarchicalTenantChildrenViaIdAsync(int tenantId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This adds a new, single level  Tenant
    /// </summary>
    /// <param name="tenantName">Name of the new single-level tenant (must be unique)</param>
    /// <param name="tenantRoleNames">Optional: List of tenant role names</param>
    /// <param name="hasOwnDb">Needed if sharding: Is true if this tenant has its own database, else false</param>
    /// <param name="databaseInfoName">This is the name of the database information in the shardingsettings file.</param>
    /// <returns>A status containing the <see cref="Tenant"/> class</returns>
    public async Task<IStatusGeneric<Tenant>> AddSingleTenantAsync(string tenantName, List<string> tenantRoleNames = null, bool? hasOwnDb = null,
        string databaseInfoName = null)
    {
        CalledMethodName = nameof(AddSingleTenantAsync);
        var status = Tenant.CreateSingleTenant(tenantName, "en".SetupAuthPLoggingLocalizer().DefaultLocalizer);
        if (status.HasErrors) return status;
        if (hasOwnDb != null)
            status.Result.UpdateShardingState(databaseInfoName, (bool)hasOwnDb);
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
    public async Task<IStatusGeneric<Tenant>> AddHierarchicalTenantAsync(string tenantName, int parentTenantId, List<string> tenantRoleNames = null,
        bool? hasOwnDb = null, string databaseInfoName = null)
    {
        CalledMethodName = nameof(AddHierarchicalTenantAsync);
        var status = Tenant.CreateHierarchicalTenant(tenantName, null, "en".SetupAuthPLoggingLocalizer().DefaultLocalizer);
        if (status.HasErrors) return status;
        if (hasOwnDb != null)
            status.Result.UpdateShardingState(databaseInfoName, (bool)hasOwnDb);
        return status;
    }

    /// <summary>
    /// This replaces the <see cref="Tenant.TenantRoles"/> in the tenant with <see param="tenantId"/> primary key
    /// </summary>
    /// <param name="tenantId">Primary key of the tenant to change</param>
    /// <param name="newTenantRoleNames">List of RoleName to replace the current tenant's <see cref="Tenant.TenantRoles"/></param>
    /// <returns></returns>
    public Task<IStatusGeneric> UpdateTenantRolesAsync(int tenantId, List<string> newTenantRoleNames)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
    /// This also means all the children underneath need to have their full name updated too
    /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
    /// to update the application's tenant data.
    /// </summary>
    /// <param name="tenantId">Primary key of the tenant to change</param>
    /// <param name="newTenantName">This is the new name for this tenant name</param>
    /// <returns></returns>
    public Task<IStatusGeneric> UpdateTenantNameAsync(int tenantId, string newTenantName)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This moves a hierarchical tenant to a new parent (which might be null). This changes the TenantFullName and the
    /// TenantDataKey of the selected tenant and all of its children
    /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
    /// to move the application's tenant data.
    /// </summary>
    /// <param name="tenantToMoveId">The primary key of the AuthP tenant to move</param>
    /// <param name="newParentTenantId">Primary key of the new parent, if 0 then you move the tenant to top</param>
    /// <returns>status</returns>
    public Task<IStatusGeneric> MoveHierarchicalTenantToAnotherParentAsync(int tenantToMoveId, int newParentTenantId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This will delete the tenant (and all its children if the data is hierarchical) and uses the <see cref="ITenantChangeService"/>,
    /// but only if no AuthP user are linked to this tenant (it will return errors listing all the AuthP user that are linked to this tenant
    /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
    /// to delete the application's tenant data.
    /// NOTE: If the tenant is hierarchical, then it will delete the tenant and all of its child tenants
    /// </summary>
    /// <param name="tenantId">The primary key of the AuthP tenant to be deleted</param>
    /// <returns>Status returning the <see cref="ITenantChangeService"/> service, in case you want copy the delete data instead of deleting</returns>
    public Task<IStatusGeneric<ITenantChangeService>> DeleteTenantAsync(int tenantId)
    {
        CalledMethodName = nameof(DeleteTenantAsync);
        var status = new StatusGenericHandler<ITenantChangeService>();
        return Task.FromResult<IStatusGeneric<ITenantChangeService>>(status);
    }

    /// <summary>
    /// This is used when sharding is enabled. It updates the tenant's <see cref="Tenant.DatabaseInfoName"/> and
    /// <see cref="Tenant.HasOwnDb"/> and calls the  <see cref="ITenantChangeService"/> <see cref="ITenantChangeService.MoveToDifferentDatabaseAsync"/>
    /// which moves the tenant data to another database and then deletes the the original tenant data.
    /// </summary>
    /// <param name="tenantToMoveId">The primary key of the AuthP tenant to be moved.
    ///     NOTE: If its a hierarchical tenant, then the tenant must be the highest parent.</param>
    /// <param name="hasOwnDb">Says whether the new database will only hold this tenant</param>
    /// <param name="databaseInfoName">The name of the connection string in the ConnectionStrings part of the appsettings file</param>
    /// <returns>status</returns>
    public Task<IStatusGeneric> MoveToDifferentDatabaseAsync(int tenantToMoveId, bool hasOwnDb, string databaseInfoName)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// This finds the roles with the given names from the AuthP database. Returns errors if not found
    /// NOTE: The Tenant checks that the role's <see cref="RoleToPermissions.RoleType"/> are valid for a tenant
    /// </summary>
    /// <param name="tenantRoleNames">List of role name. Can be null, which means no roles to add</param>
    /// <returns>Status</returns>
    public Task<IStatusGeneric<List<RoleToPermissions>>> GetRolesWithChecksAsync(List<string> tenantRoleNames)
    {
        throw new NotImplementedException();
    }
}