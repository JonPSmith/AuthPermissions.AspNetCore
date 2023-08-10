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
/// NOTE: if your tenants are ALL using sharding, then set <see cref="HybridMode"/> parameter to false
/// </summary>
public class ShardingEntryOptions : ShardingEntry
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="hybridMode">If true, then you can add shared tenants into the database used by AuthP.
    /// If false, then you are in sharding-only mode, where you can't add tenants into the database used by AuthP.
    /// </param>
    public ShardingEntryOptions(bool hybridMode)
    {
        HybridMode = hybridMode;
    }

    /// <summary>
    /// If true, then
    /// - A default <see cref = "ShardingEntry" /> is added to the sharding entities
    ///   which allowing tenants to be added to the AuthP's context.
    /// - The DefaultConnection connection string is shown in the <see cref="IGetSetShardingEntries.GetConnectionStringNames"/> method
    /// If false, then
    /// - The sharding entities start as empty
    /// - The DefaultConnection connection string is not shown in the <see cref="IGetSetShardingEntries.GetConnectionStringNames"/> method
    /// </summary>
    public bool HybridMode { get; private set; }


    /// <summary>
    /// This holds the name of the DefaultConnection in the ConnectionStrings in the appsettings.json file.
    /// If the <see cref="HybridMode"/> is false, then the <see cref="IGetSetShardingEntries"/> will
    /// remove this named connection string 
    /// </summary>
    public string DefaultConnectionName { get; set; } = "DefaultConnection";

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
    /// Can be an empty if <see cref="HybridMode"/> is false (useful in sharding only situations)
    /// </summary>
    /// <param name="options">Needed to fill in the <see cref="ShardingEntryOptions."/></param>
    /// <param name="authPContext">Optional: Only needed if AddIfEmpty and using custom database.
    /// You must provide the <see cref="AuthPermissionsDbContext"/> to get the short provider name.</param>
    /// <returns></returns>
    public ShardingEntry ProvideDefaultShardingEntry(
        AuthPermissionsOptions options, AuthPermissionsDbContext authPContext = null)
    {
        if (!HybridMode)
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