// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This defines the default sharding entry to add the sharding if the sharding data is empty.
/// You can manually set the four properties and/or call the <see cref="FormDefaultDatabaseInfo"/>
/// method to fill in the normal default <see cref="DatabaseInformation"/>.
/// NOTE: if your tenants are ALL using sharding, then set <see cref="AddIfEmpty"/> parameter to false
/// </summary>
public class DatabaseInformationOptions : DatabaseInformation
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="addIfEmpty">defaults to true. Set this to false if all of your tenant are shading.</param>
    public DatabaseInformationOptions(bool addIfEmpty = true)
    {
        AddIfEmpty = addIfEmpty;
    }

    /// <summary>
    /// if your tenants are ALL using sharding, then set this property to false.
    /// That's because when only sharding tenants the code will add (and remove) a sharding entry.
    /// </summary>
    public bool AddIfEmpty { get; private set; }

    /// <summary>
    /// This fills in the <see cref="DatabaseInformation"/> with the default information.
    /// NOTE: If you 
    /// </summary>
    /// <param name="options">This is used to set the <see cref="DatabaseInformation.DatabaseType"/>
    /// based on which database provider you selected. NOTE: If using custom database, then you MUST
    /// define the <see cref="DatabaseInformation.DatabaseType"/> with the short form of the custom
    /// database provider name before you call this method.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void FormDefaultDatabaseInfo(AuthPermissionsOptions options)
    {
        //Set up the default settings
        Name ??= options.ShardingDefaultDatabaseInfoName ??
               throw new ArgumentNullException(nameof(options.ShardingDefaultDatabaseInfoName));
        ConnectionName ??= "DefaultConnection";
        DatabaseType ??= GetShortDatabaseProviderName(options);
    }

    /// <summary>
    /// This return the correct list of default <see cref="DatabaseInformation"/> list.
    /// Can be an empty if <see cref="AddIfEmpty"/> is false (useful in sharding only situations)
    /// </summary>
    /// <returns></returns>
    public List<DatabaseInformation> ProvideEmptyDefaultDatabaseInformations()
    {
        return AddIfEmpty
            ? new List<DatabaseInformation> { this }
            : new List<DatabaseInformation>(); //Empty - used when all tenants have their own database, i.e. all sharding
    }

    private string GetShortDatabaseProviderName(AuthPermissionsOptions options)
    {
        switch (options.InternalData.AuthPDatabaseType)
        {
            case AuthPDatabaseTypes.NotSet:
                throw new AuthPermissionsException("You have not set the database provider.");
            case AuthPDatabaseTypes.SqliteInMemory:
                return "Sqlite";
            case AuthPDatabaseTypes.SqlServer:
                return "SqlServer";
            case AuthPDatabaseTypes.PostgreSQL:
                return "PostgreSQL";
            case AuthPDatabaseTypes.CustomDatabase:
                if (!AddIfEmpty)
                    throw new AuthPermissionsException("You are using custom database, so you set the DatabaseType" +
                                                   " to the short form of the database provider name, e.g. SqlServer.");
                return null;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}