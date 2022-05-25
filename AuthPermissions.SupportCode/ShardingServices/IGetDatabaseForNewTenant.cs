// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace AuthPermissions.SupportCode.ShardingServices;

/// <summary>
/// This defined a service that will find a database for a new tenant when using sharding.
/// </summary>
public interface IGetDatabaseForNewTenant
{
    /// <summary>
    /// This will look for a database for a new tenant when <see cref="TenantTypes.AddSharding"/> is on
    /// The job of this method that will return a DatabaseInfoName for the database to use, or an error if can't be found
    /// </summary>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    Task<IStatusGeneric<string>> FindBestDatabaseInfoNameAsync(bool hasOwnDb, string region);
}