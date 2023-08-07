// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This holds user data (via the inherit of the <see cref="AddNewUserDto"/>)
/// and the Tenant information.
/// </summary>
public class AddNewTenantDto
{
    /// <summary>
    /// This is the name of the new tenant the user wants to create
    /// Must be provided
    /// </summary>
    public string TenantName { get; set; }

    /// <summary>
    /// This holds the name of the version of the multi-tenant features the user has selected
    /// Can be null if not using versions 
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// If the <see cref="MultiTenantVersionData"/>.<see cref="MultiTenantVersionData.HasOwnDbForEachVersion"/> is null
    /// and <see cref="TenantTypes.AddSharding"/> is on, then you must set this to true / false.
    /// true means the new tenant has its own database, while false means the database will contain multiple tenants
    /// NOTE: If the <see cref="MultiTenantVersionData"/>.<see cref="MultiTenantVersionData.HasOwnDbForEachVersion"/> isn't null
    /// then this parameter is ignored.
    /// </summary>
    public bool? HasOwnDb { get; set; }

    /// <summary>
    /// Optional: If <see cref="TenantTypes.AddSharding"/> and you have database servers geographically spread,
    /// then you can provide some information to help the <see cref="IGetDatabaseForNewTenant"/> service
    /// to pick the right server/database.
    /// Can be null.
    /// </summary>
    public string Region { get; set; }

    /// <summary>
    /// Optional: A list of regions for the user to pick from.
    /// </summary>
    public List<string> PossibleRegions { get; set; }
}