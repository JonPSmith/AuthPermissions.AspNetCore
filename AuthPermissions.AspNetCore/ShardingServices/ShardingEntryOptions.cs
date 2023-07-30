// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This defines the default sharding entry to add the sharding if the sharding data is empty.
/// You can manually set the four properties and/or call the <see cref="FormDefaultShardingEntry"/>
/// method to fill in the normal default <see cref="ShardingEntry"/>.
/// NOTE: if your tenants are ALL using sharding, then set <see cref="AddIfEmpty"/> parameter to false
/// </summary>
public class ShardingEntryOptions : ShardingEntry
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="addIfEmpty">defaults to true. Set this to false if all of your tenant are shading.</param>
    public ShardingEntryOptions(bool addIfEmpty = true)
    {
        AddIfEmpty = addIfEmpty;
    }

    /// <summary>
    /// if your tenants are ALL using sharding, then set this property to false.
    /// That's because when only sharding tenants the code will add (and remove) a sharding entry.
    /// </summary>
    public bool AddIfEmpty { get; private set; }

    /// <summary>
    /// This fills in the <see cref="ShardingEntry"/> with the default information.
    /// NOTE: If you 
    /// </summary>
    /// <param name="options">This is used to set the <see cref="ShardingEntry.DatabaseType"/>
    /// based on which database provider you selected. NOTE: If using custom database, then you MUST
    /// define the <see cref="ShardingEntry.DatabaseType"/> with the short form of the custom
    /// database provider name before you call this method.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void FormDefaultShardingEntry(AuthPermissionsOptions options, AuthPermissionsDbContext authPContext = null)
    {
        //Set up the default settings
        Name ??= options.DefaultShardingEntryName ??
               throw new ArgumentNullException(nameof(options.DefaultShardingEntryName));
        ConnectionName ??= "DefaultConnection";
        DatabaseType ??= GetShortDatabaseProviderName(options, authPContext)
            ?? throw new ArgumentNullException(nameof(options.DefaultShardingEntryName),
                $"You must the provide a {nameof(DatabaseType)} if you are going call the {nameof(FormDefaultShardingEntry)} method.");
    }

    /// <summary>
    /// This return the correct list of default <see cref="ShardingEntry"/> list.
    /// Can be an empty if <see cref="AddIfEmpty"/> is false (useful in sharding only situations)
    /// </summary>
    /// <param name="options">Needed to fill in the <see cref="ShardingEntryOptions.DatabaseType"/></param>
    /// <param name="authPContext">Optional: Only needed if AddIfEmpty and using custom database.
    /// You must provide the <see cref="AuthPermissionsDbContext"/> to get the short provider name.</param>
    /// <returns></returns>
    public ShardingEntry ProvideDefaultShardingEntry(
        AuthPermissionsOptions options, AuthPermissionsDbContext authPContext = null)
    {
        if (!AddIfEmpty)
            return null; //Empty - used when all tenants have their own database, i.e. all sharding
        
        FormDefaultShardingEntry(options, authPContext);
        return this;
    }

    private string GetShortDatabaseProviderName(AuthPermissionsOptions options, AuthPermissionsDbContext authPContext = null)
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
                return authPContext.GetProviderShortName();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}