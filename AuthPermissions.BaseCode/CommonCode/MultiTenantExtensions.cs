// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;

namespace AuthPermissions.BaseCode.CommonCode;

/// <summary>
/// Extension methods to 
/// </summary>
public static class MultiTenantExtensions
{
    /// <summary>
    /// If the DataKey contains this string, then the single-level query filter should be set to true
    /// </summary>
    public const string DataKeyNoQueryFilter = "NoQueryFilter";

    /// <summary>
    /// This calculates the data key from the tenantId and the parentDataKey.
    /// If it is a single layer multi-tenant it will by the TenantId as a string
    ///    - If the tenant is in its own database, then it will send back the <see cref="DataKeyNoQueryFilter"/> constant
    /// If it is a hierarchical multi-tenant it will contains a concatenation of the tenantsId in the parents as well
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="parentDataKey">The parentDataKey is needed if hierarchical</param>
    /// <param name="isHierarchical"></param>
    /// <param name="hasItsOwnDb"></param>
    public static string GetTenantDataKey(this int tenantId, string parentDataKey, bool isHierarchical, bool hasItsOwnDb)
    {
        if (tenantId == default)
            throw new AuthPermissionsException(
                "The Tenant DataKey is only correct if the tenant primary key is set");

        
        return isHierarchical || !hasItsOwnDb
            ? parentDataKey + $"{tenantId}." //This works for single-level because the parentDataKey is null in that case
            : DataKeyNoQueryFilter;
    }

    /// <summary>
    /// This calculates the data key for given tenant.
    /// If it is a single layer multi-tenant it will by the TenantId as a string
    /// If it is a hierarchical multi-tenant it will contains a concatenation of the tenantsId in the parents as well
    /// </summary>
    /// <param name="tenant"></param>
    /// <returns></returns>
    public static string GetTenantDataKey(this Tenant tenant)
    {
        return tenant.TenantId.GetTenantDataKey(tenant.ParentDataKey, tenant.IsHierarchical, tenant.HasOwnDb);
    }

    /// <summary>
    /// This returns true if the Tenant is using sharding
    /// </summary>
    /// <param name="tenant"></param>
    /// <returns></returns>
    public static bool IsSharding(this Tenant tenant)
    {
        return tenant.DatabaseInfoName != null;
    }

    /// <summary>
    /// This returns the highest TenantId for a tenant
    /// This is used if a tenant is moved to another database, as we must move all the hierarchical data
    /// - For single-level multi-tenant, this will be the TenantId
    /// - for hierarchical multi-tenant, this will be the first TenantId in the ParentDataKey,
    ///   or this TenantId if the ParentDataKey is null
    /// </summary>
    /// <returns>The highest TenantId of a tenant</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public static int GetHighestTenantId(this int tenantId, string parentDataKey = null)
    {
        if (tenantId == default)
            throw new AuthPermissionsException(
                "The Tenant DataKey is only correct if the tenant primary key is set");

        //NOTE: If single-level, then ParentDataKey will be null
        return parentDataKey == null
            ? tenantId
            : int.Parse(parentDataKey.Substring(0, parentDataKey.IndexOf('.')));
    }
}