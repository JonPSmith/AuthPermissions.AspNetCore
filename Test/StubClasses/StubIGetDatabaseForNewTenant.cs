// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace Test.StubClasses;

public class StubIGetDatabaseForNewTenant : IGetDatabaseForNewTenant
{
    private readonly AuthPermissionsDbContext _context;
    private readonly bool _returnError;

    public StubIGetDatabaseForNewTenant(AuthPermissionsDbContext context, bool returnError)
    {
        _context = context;
        _returnError = returnError;
    }
    private Tenant _tenant;

    public bool RemoveLastDatabaseCalled { get; private set; }

    /// <summary>
    /// This will look for a database for a new tenant when <see cref="TenantTypes.AddSharding"/> is on
    /// The job of this method is to find or create a database. If it successful, then it
    /// a) Gets the DatabaseInfoName of the entry for the database we are using
    /// b) It updates the Tenant's sharding information
    /// </summary>
    /// <param name="tenant"></param>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <param name="version">Optional: provides the version name in case that effects the database selection</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public Task<IStatusGeneric<Tenant>> FindOrCreateDatabaseAsync(Tenant tenant, bool hasOwnDb, string region,
        string version)
    {
        _tenant = tenant;
        var status = new StatusGenericHandler<Tenant>();
        if (_returnError)
            return Task.FromResult(status.AddError("An Error"));

        tenant.UpdateShardingState(hasOwnDb ? "OwnDb" : "SharedDb", hasOwnDb); 
        _context.SaveChanges();

        return Task.FromResult<IStatusGeneric<Tenant>>(status.SetResult(tenant));
    }

    /// <summary>
    /// If called it will undo what the <see cref="IGetDatabaseForNewTenant.FindOrCreateDatabaseAsync"/> did.
    /// This is called if there was a problem with the new user such that the new tenant would be removed.
    /// </summary>
    /// <returns></returns>
    public Task<IStatusGeneric> RemoveLastDatabaseSetupAsync()
    {
        RemoveLastDatabaseCalled = true;
        _context.Tenants.Remove(_tenant);
        return Task.FromResult<IStatusGeneric>(new StatusGenericHandler());
    }
}