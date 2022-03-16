// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.CommonCode;
using AuthPermissions.SetupCode;

namespace AuthPermissions.AdminCode;

public static class TenantTypeExtensions
{
    public static void ThrowExceptionIfTenantTypeIsWrong(this TenantTypes tenantType)
    {
        if (tenantType.HasFlag(TenantTypes.SingleLevel) && tenantType.HasFlag(TenantTypes.HierarchicalTenant))
            throw new AuthPermissionsException(
                $"The {nameof(AuthPermissionsOptions.TenantType)} option can't have {nameof(TenantTypes.SingleLevel)} and "+
                $"{nameof(TenantTypes.HierarchicalTenant)} at the same time.");

        if (!tenantType.IsMultiTenant() && tenantType.HasFlag(TenantTypes.AddSharding))
            throw new AuthPermissionsException(
                $"You need to set the {nameof(AuthPermissionsOptions.TenantType)} option to either {nameof(TenantTypes.SingleLevel)} or " +
                $"{nameof(TenantTypes.HierarchicalTenant)} when setting the {nameof(TenantTypes.AddSharding)} flag.");

    }

    public static bool IsMultiTenant(this TenantTypes tenantType)
    {
        return tenantType.HasFlag(TenantTypes.SingleLevel) || tenantType.HasFlag(TenantTypes.HierarchicalTenant);
    }

    public static bool IsSingleLevel(this TenantTypes tenantType)
    {
        return tenantType.HasFlag(TenantTypes.SingleLevel);
    }

    public static bool IsHierarchical(this TenantTypes tenantType)
    {
        return tenantType.HasFlag(TenantTypes.HierarchicalTenant);
    }

    public static bool UsingSharding(this TenantTypes tenantType)
    {
        return tenantType.HasFlag(TenantTypes.AddSharding);
    }
}