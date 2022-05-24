// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using StatusGeneric;

namespace AuthPermissions.SupportCode.ShardingServices;

/// <summary>
/// This defined a service that will find a database for a new tenant when using sharding.
/// </summary>
public interface IGetDatabaseForNewTenant
{
    /// <summary>
    /// This will look for a database for a new tenant.
    /// If the hasOwnDb is true, then it will find an empty database
    /// </summary>
    /// <param name="hasOwnDb"></param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    Task<IStatusGeneric<string>> FindBestDatabaseInfoNameAsync(bool hasOwnDb);
}