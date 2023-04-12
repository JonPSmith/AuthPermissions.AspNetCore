// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This class holds the information about each database used by the AuthP sharding feature
/// The <see cref="ShardingSettingsOption"/> class has an array of <see cref="DatabaseInformation"/> 
/// </summary>
public class DatabaseInformation
{
    /// <summary>
    /// This holds the name for this database information, which will be seen by admin users and in a claim
    /// This is used as a reference to this <see cref="DatabaseInformation"/>
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
    /// This defines the type of database. If not set, then is default value is "SqlServer"
    /// </summary>
    public string DatabaseType { get; set; }

    /// <summary>
    /// This creates a default <see cref="DatabaseInformation"/> class. This is used if there is no sharding settings file
    /// </summary>
    /// <param name="options">Uses information in the AuthP's options to define the default settings</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DatabaseInformation FormDefaultDatabaseInfo(AuthPermissionsOptions options)
    {
        return new DatabaseInformation
        {
            Name = options.ShardingDefaultDatabaseInfoName ?? throw new ArgumentNullException(nameof(options.ShardingDefaultDatabaseInfoName)),
            ConnectionName = "DefaultConnection",
            DatabaseType = options.InternalData.AuthPDatabaseType.ToString(),
        };
    }

    /// <summary>
    /// Useful for debugging
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(DatabaseName)}: {DatabaseName ?? " < null > "}, {nameof(ConnectionName)}: {ConnectionName}, {nameof(DatabaseType)}: {DatabaseType}";
    }
}