// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using Microsoft.IdentityModel.Tokens;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This is used if you want to use the <see cref="IShardingOnlyTenantAddRemove"/>'s
/// <see cref="ShardingOnlyTenantAddRemove.CreateTenantAsync"/>.
/// </summary>
public class ShardingOnlyTenantAddDto
{
    /// <summary>
    /// Required: The name of the new tenant to create
    /// </summary>
    public string TenantName { get; set; }

    /// <summary>
    /// Defines if the tenant should have its own database - always true
    /// </summary>
    public bool HasOwnDb => true;

    /// <summary>
    /// Optional: If adding a child hierarchical, then this must be set to a id of the parent hierarchical tenant
    /// </summary>
    public int ParentTenantId { get; set; } = 0;

    /// <summary>
    /// Optional: List of tenant role names 
    /// </summary>
    public List<string> TenantRoleNames { get; set; } = new List<string>();

    /// <summary>
    /// Optional: If you have multiple connections strings you should This should contains the names of the connection strings to select the correct server
    /// for the 
    /// </summary>
    public List<string> ConnectionStringNames { get; set; }

    /// <summary>
    /// Required: The name of the connection string which defines the database server to use
    /// </summary>
    public string ConnectionStringName { get; set; }

    /// <summary>
    /// The short name (e.g. SqlServer) of the database provider for this tenant
    /// </summary>
    public string DbProviderShortName { get; set; }

    /// <summary>
    /// THis 
    /// </summary>
    /// <param name="connectionStringNames"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetConnectionStringNames(List<string> connectionStringNames)
    {
        if (connectionStringNames == null || connectionStringNames.Count == 0)
            throw new ArgumentException($"The list of connection string names cannot be null or empty collection.", nameof(connectionStringNames));


        ConnectionStringNames = connectionStringNames;
        ConnectionStringName = connectionStringNames.First();
    }

    /// <summary>
    /// This ensures the data provided is valid
    /// </summary>
    /// <exception cref="AuthPermissionsBadDataException"></exception>
    public void ValidateProperties()
    {
        if (TenantName.IsNullOrEmpty())
            throw new AuthPermissionsBadDataException("Should not be null or empty", nameof(TenantName));

        if (ConnectionStringName.IsNullOrEmpty())
            throw new AuthPermissionsBadDataException("Should not be null or empty", nameof(ConnectionStringName));

        if (DbProviderShortName.IsNullOrEmpty())
            throw new AuthPermissionsBadDataException("Should not be null or empty", nameof(DbProviderShortName));
    }

    /// <summary>
    /// This will build the <see cref="ShardingEntry"/> when you add a shard tenant.
    /// NOTE: I have used a datetime for the database name for the reasons covered in the comments.
    /// If you want to change the <see cref="ShardingEntry"/>'s Name or the DatabaseName,
    /// then you can create a new class and override the <see cref="FormDatabaseInformation"/> method.
    /// See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/override
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public virtual ShardingEntry FormDatabaseInformation()
    {
        var dateTimeNow = DateTime.UtcNow;
        return new ShardingEntry
        {
            //NOTE: I don't include the tenant name in the database name because
            //1. The tenant name can be changed, but you can't always the change the database name 
            //2. PostgreSQL has a 64 character limit on the name of a database
            Name = $"{TenantName}-{dateTimeNow.ToString("yyyyMMddHHmmss")}",
            DatabaseName = dateTimeNow.ToString("yyyyMMddHHmmss-fff"),
            ConnectionName = ConnectionStringName,
            DatabaseType = DbProviderShortName
        };
    }
}