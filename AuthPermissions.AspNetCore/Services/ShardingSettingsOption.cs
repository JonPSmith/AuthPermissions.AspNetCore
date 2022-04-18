// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AuthPermissions.AspNetCore.Services;

public class ShardingSettingsOption
{
    /// <summary>
    /// The name of the section where the sharding data is held
    /// </summary>
    public const string SectionName = "ShardingData";

    /// <summary>
    /// This holds the list of <see cref="ShardingDatabaseData"/>. Can be null if no 
    /// </summary>
    public List<ShardingDatabaseData> ShardingDatabases { get; set; }
}

/// <summary>
/// This class holds the information about each database used by the AuthP sharding feature
/// </summary>
public class ShardingDatabaseData
{
    /// <summary>
    /// This holds the name for this sharding database. It should not be null
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// This contains the name of the database. Can be null, in which case it will use the database name found in the connection string
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// This contains the name of the connection string in the appsettings' "ConnectionStrings" part
    /// If not set, then is is set to "DefaultConnection"
    /// </summary>
    public string ConnectionName { get; set; } = "DefaultConnection";

    /// <summary>
    /// This defines the type of database. If not set, then is is set to "SqlServer"
    /// </summary>
    public string DatabaseType { get; set; } = "SqlServer";

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(DatabaseName)}: {DatabaseName ?? " < null > "}, {nameof(ConnectionName)}: {ConnectionName}, {nameof(DatabaseType)}: {DatabaseType}";
    }
}