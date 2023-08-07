// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.SupportCode.AddUsersServices;

namespace Example7.MvcWebApp.ShardingOnly.PermissionsCode;

public static class Example7CreateTenantVersions
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
        },
        HasOwnDbForEachVersion = new Dictionary<string, bool?>()
        {
            { "Free", true },
            { "Pro", true },
            { "Enterprise", true }
        }

    };
}