// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This defined a service that will find a database for a new tenant when using sharding.
/// </summary>
public interface IGetDatabaseForNewTenant
{
    /// <summary>
    /// This will look for a database for a new tenant when <see cref="TenantTypes.AddSharding"/> is on
    /// The job of this method is to find or create a database. If it successful, then it
    /// a) Gets the DatabaseInfoName of the entry for the database we are using
    /// b) It updates the Tenant's sharding information
    /// </summary>
    /// <param name="tenant">This is the tenant that you want to find/create a new database.</param>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <param name="version">Optional: provides the version name in case that effects the database selection</param>
    /// <returns>Status with the updated tenant, or error if it can't find a database to work with</returns>
    Task<IStatusGeneric<Tenant>> FindOrCreateDatabaseAsync(Tenant tenant, bool hasOwnDb, string region, string version = null);

    /// <summary>
    /// If called it will undo what the <see cref="FindOrCreateDatabaseAsync"/> did,
    /// i.e. deleting the database (or at least use tenantChangeService to delete the data) and remove the sharding data.  
    /// This is called if there was a problem with the new user such that the new tenant would be deleted.
    /// NOTE: This method may be called even if the <see cref="FindOrCreateDatabaseAsync"/> hasn't been called.
    /// </summary>
    /// <returns>Status</returns>
    Task<IStatusGeneric> RemoveLastDatabaseSetupAsync();
}