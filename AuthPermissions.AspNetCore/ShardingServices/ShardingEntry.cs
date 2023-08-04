// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This class holds the information about each database used by the AuthP sharding feature
/// </summary>
public class ShardingEntry
{
    /// <summary>
    /// This holds the name for this database information, which will be seen by admin users and in a claim
    /// This is used as a reference to this <see cref="ShardingEntry"/>
    /// The <see cref="Name"/> should not be null, and should be unique
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// This contains the name of the database. Can be null or empty string, in which case it will use the database name found in the connection string
    /// NOTE: for some reason the <see cref="DatabaseName"/> is an empty string, when the actual json says it is null
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// This contains the name of the connection string in the appsettings' "ConnectionStrings" part
    /// If not set, then is default value is "DefaultConnection"
    /// </summary>
    public string ConnectionName { get; set; }

    /// <summary>
    /// This defines the short name of the of database provider, e,g. "SqlServer".
    /// </summary>
    public string DatabaseType { get; set; }

    /// <summary>
    /// Useful for debugging
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(DatabaseName)}: {DatabaseName ?? " < null > "}, {nameof(ConnectionName)}: {ConnectionName}, {nameof(DatabaseType)}: {DatabaseType}";
    }
}