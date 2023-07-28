// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.CommonCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// Simple method to get the short name of a EF Core database provider
/// </summary>
public static class ProviderNameExtension
{
    /// <summary>
    /// This returns the short name that people know these EF Core databases
    /// NOTE: This only works for the primary EF Core database providers 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetProviderShortName(this DbContext context)
    {
        if (context == null)
            throw new AuthPermissionsException(
                "When using a custom database provide, then you must provide an instance of the AuthPermissionsDbContext context.");

        var lastPart = context.Database.ProviderName!.Split('.').Last();
        if (lastPart == "EntityFrameworkCore")
        {
            //Its either MySql.EntityFrameworkCore or Oracle.EntityFrameworkCore
            return context.Database.ProviderName!.Split('.').First();
        }
        return lastPart;
    }
}