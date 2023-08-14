// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;

namespace Example6.MvcWebApp.Sharding.Models;

public class ShardingEntryEdit 
{
    public ShardingEntry DatabaseInfo { get; set; }

    public IEnumerable<string> AllPossibleConnectionNames { get; set; }

    public string[] PossibleDatabaseTypes { get; set; }
}