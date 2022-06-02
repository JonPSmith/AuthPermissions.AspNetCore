// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.SupportCode.AddUsersServices;

namespace Example3.MvcWebApp.IndividualAccounts.PermissionsCode;

public static class Example3CreateTenantVersions
{
    public static readonly MultiTenantVersionData TenantSetupData = new()
    {
        TenantRolesForEachVersion = new Dictionary<string, List<string>>()
        {
            { "Free", null },
            { "Pro", new List<string> { "Tenant Admin" } },
            { "Enterprise", new List<string> { "Tenant Admin", "Enterprise" } },
        },
        TenantAdminRoles = new Dictionary<string, List<string>>()
        {
            { "Free", new List<string> { "Invoice Reader", "Invoice Creator" } },
            { "Pro", new List<string> { "Invoice Reader", "Invoice Creator", "Tenant Admin" } },
            { "Enterprise", new List<string> { "Invoice Reader", "Invoice Creator", "Tenant Admin" } }
        }
        //No settings for HasOwnDbForEachVersion as this isn't a sharding 
    };
}