// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;

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
    /// This defines the short name of the of database provider, e,g. "SqlServer".
    /// </summary>
    public string DatabaseType { get; set; }

    /// <summary>
    /// This creates a default <see cref="DatabaseInformation"/> class. This is used if there is no sharding settings file
    /// </summary>
    /// <param name="options">Uses information in the AuthP's options to define the default settings</param>
    /// <param name="authPContext">OPTIONAL: if using a custom database provider, then you must provide the
    /// <see cref="AuthPermissionsDbContext"/> instance so that it can get the .</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DatabaseInformation FormDefaultDatabaseInfo(AuthPermissionsOptions options, 
        AuthPermissionsDbContext authPContext = null)
    {
        string GetShortDatabaseProviderName()
        {
            switch (options.InternalData.AuthPDatabaseType)
            {
                case AuthPDatabaseTypes.NotSet:
                    throw new AuthPermissionsException("You have not set the database provider.");
                case AuthPDatabaseTypes.SqliteInMemory:
                    return "Sqlite";
                case AuthPDatabaseTypes.SqlServer:
                    return "SqlServer";
                case AuthPDatabaseTypes.Postgres:
                    return "PostgreSQL";
                case AuthPDatabaseTypes.CustomDatabase:
                    if(authPContext == null)
                        throw new AuthPermissionsException(
                            "When using a custom database provide, then you must provide an instance of the AuthPermissionsDbContext context.");
                    return authPContext.GetProviderShortName();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return new DatabaseInformation
        {
            Name = options.ShardingDefaultDatabaseInfoName ?? throw new ArgumentNullException(nameof(options.ShardingDefaultDatabaseInfoName)),
            ConnectionName = "DefaultConnection",
            DatabaseType = GetShortDatabaseProviderName()
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