﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;

namespace Example7.MvcWebApp.ShardingOnly.Models;

public class ShardingEntryEdit 
{
    public ShardingEntry DatabaseInfo { get; set; }

    public IEnumerable<string> AllPossibleConnectionNames { get; set; }

    public string[] PossibleDatabaseTypes { get; set; }
}