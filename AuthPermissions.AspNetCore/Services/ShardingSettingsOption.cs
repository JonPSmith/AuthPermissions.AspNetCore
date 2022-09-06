// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// This contains the data in the sharding settings file
/// </summary>
public class ShardingSettingsOption
{
    /// <summary>
    /// This holds the list of <see cref="DatabaseInformation"/>. Can be null if no 
    /// </summary>
    public List<DatabaseInformation> ShardingDatabases { get; set; }
}