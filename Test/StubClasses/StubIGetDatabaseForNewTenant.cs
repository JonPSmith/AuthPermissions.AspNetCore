// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.ShardingServices;
using StatusGeneric;

namespace Test.StubClasses;

public class StubIGetDatabaseForNewTenant : IGetDatabaseForNewTenant
{
    /// <summary>
    /// This will look for a database for a new tenant when <see cref="TenantTypes.AddSharding"/> is on
    /// The job of this method that will return a DatabaseInfoName for the database to use, or an error if can't be found
    /// </summary>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <param name="version">Optional: provides the version name in case that effects the database selection</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public Task<IStatusGeneric<string>> FindBestDatabaseInfoNameAsync(bool hasOwnDb, string region, string version)
    {
        var status = new StatusGenericHandler<string>();
        status.SetResult(hasOwnDb ? "OwnDb" : "SharedDb");
        return Task.FromResult<IStatusGeneric<string>>(status);
    }
}