// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Microsoft.IdentityModel.Tokens;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This service is designed for applications using the <see cref="TenantTypes.AddSharding"/>
/// Each tenant (shared or shard) needs a <see cref="ShardingEntry"/> entry to define the database
/// before you can create the tenant. And for delete of a shard tenant it will remove the
/// <see cref="ShardingEntry"/> entry if its <see cref="Tenant.HasOwnDb"/> is true.
/// </summary>
public class ShardingTenantAddRemove : IShardingTenantAddRemove
{
    private readonly IAuthTenantAdminService _tenantAdmin;
    private readonly IGetSetShardingEntries _getSetShardings;
    private readonly AuthPermissionsOptions _options;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
    public ShardingTenantAddRemove(IAuthTenantAdminService tenantAdmin,
        IGetSetShardingEntries getSetShardings, 
        AuthPermissionsOptions options, IAuthPDefaultLocalizer localizeProvider)
    {
        _tenantAdmin = tenantAdmin ?? throw new ArgumentNullException(nameof(tenantAdmin));
        _getSetShardings = getSetShardings ?? throw new ArgumentNullException(nameof(getSetShardings));
        _options = options;
        _localizeDefault = localizeProvider.DefaultLocalizer;

        if (!_options.TenantType.IsSharding())
            throw new AuthPermissionsException("This service is specifically designed for sharding multi-tenants " +
                                               "and you are not using sharding.");
    }

    /// <summary>
    /// This creates a tenant (shared or shard), and if that tenant is a shard (i.e. has its own database)
    /// it will create a sharding entry to contain the new database name
    /// (unless the <see cref="ShardingTenantAddDto.ShardingEntityName"/> isn't empty, when it will lookup the
    /// <see cref="ShardingEntry"/> defined by the <see cref="ShardingTenantAddDto.ShardingEntityName"/>).
    /// If a tenant that shares a database (tenant's HasOwnDb properly is false), then it use the <see cref="ShardingEntry"/> defined by the
    /// <see cref="ShardingTenantAddDto.ShardingEntityName"/> in the <see cref="ShardingTenantAddDto"/>.
    /// </summary>
    /// <param name="dto">A class called <see cref="ShardingTenantAddDto"/> holds all the data needed,
    /// including a method to validate that the information is correct.</param>
    /// <returns>status</returns>
    public async Task<IStatusGeneric> CreateTenantAsync(ShardingTenantAddDto dto)
    {
        dto.ValidateProperties();
        if (_options.TenantType.IsSingleLevel() && dto.ParentTenantId != 0)
            throw new AuthPermissionsException("The parentTenantId parameter must be zero if for SingleLevel.");

        var status = new StatusGenericLocalizer(_localizeDefault);
        if (_tenantAdmin.QueryTenants().Any(x => x.TenantFullName == dto.TenantName))
            return status.AddErrorFormattedWithParams("DuplicateTenantName".ClassLocalizeKey(this, true),
                $"The tenant name '{dto.TenantName}' is already used", nameof(dto.TenantName));

        //1. We obtain an information data via the ShardingTenantAddDto class
        ShardingEntry databaseInfo = null;
        if (_options.TenantType.IsHierarchical() && dto.ParentTenantId != 0)
        {
            //if a child hierarchical tenant we don't need to get the ShardingEntry as the parent's ShardingEntry is used
            var parentStatus = await _tenantAdmin.GetTenantViaIdAsync(dto.ParentTenantId);
            if (status.CombineStatuses(parentStatus).HasErrors)
                return status;

            databaseInfo = _getSetShardings.GetSingleShardingEntry(parentStatus.Result.DatabaseInfoName);
            if (databaseInfo == null)
                return status.AddErrorFormatted("MissingDatabaseInformation".ClassLocalizeKey(this, true),
                    $"The ShardingEntry for the parent '{parentStatus.Result.TenantFullName}' wasn't found.");
        }
        else if (!dto.ShardingEntityName.IsNullOrEmpty())
        {
            //The DatabaseInfoName has been set, so get the ShardingEntry 
            databaseInfo = _getSetShardings.GetSingleShardingEntry(dto.ShardingEntityName);

            if (databaseInfo == null)
                throw new AuthPermissionsException(
                    $"The {nameof(ShardingTenantAddDto.ShardingEntityName)} you provided wasn't found in the sharding entries.");
        }
        else if (dto.HasOwnDb == true)
        {
            //Its a new sharding tenant, so we need to create a new ShardingEntry entry for this database
            databaseInfo = dto.FormDatabaseInformation();
            if (status.CombineStatuses(
                    _getSetShardings.AddNewShardingEntry(databaseInfo)).HasErrors)
                return status;
        }

        //2. Now we can create the tenant, which in turn will setup the database via your ITenantChangeService implementation
        if (_options.TenantType.IsSingleLevel())
            status.CombineStatuses(await _tenantAdmin.AddSingleTenantAsync(dto.TenantName, dto.TenantRoleNames,
                dto.HasOwnDb, databaseInfo?.Name));
        else
        {
            status.CombineStatuses(await _tenantAdmin.AddHierarchicalTenantAsync(dto.TenantName,
                dto.ParentTenantId, dto.TenantRoleNames,
                dto.HasOwnDb, databaseInfo?.Name));
        }

        if (status.HasErrors && dto.HasOwnDb == true)
        {
            //we created a ShardingEntry, so we want to delete it
            status.CombineStatuses(
                _getSetShardings.RemoveShardingEntry(databaseInfo.Name));
        }

        return status;
    }

    /// <summary>
    /// This will delete a tenant (shared or shard), and if that tenant <see cref="Tenant.HasOwnDb"/> is true
    /// it will also delete the <see cref="ShardingEntry"/> entry for this shard tenant.
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    /// <returns>status</returns>
    public async Task<IStatusGeneric> DeleteTenantAsync(int tenantId)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        //1. We find the tenant to get HasOwnDb. If true, then we hold the DatabaseInfoName to delete the sharding entry
        var tenantStatus = await _tenantAdmin.GetTenantViaIdAsync(tenantId);
        if (status.CombineStatuses(tenantStatus).HasErrors)
            return status;

        string? databaseInfoName = tenantStatus.Result.HasOwnDb && tenantStatus.Result.ParentTenantId == null
            ? tenantStatus.Result.DatabaseInfoName
            : null;

        //2. We delete the tenant
        if (status.CombineStatuses(await _tenantAdmin.DeleteTenantAsync(tenantId)).HasErrors)
            return status;

        //3. If the tenant was successfully deleted, and the tenant's HasOwnDb is true, then we delete the ShardingEntry
        if (databaseInfoName != null)
        {
            //We ignore any errors
            _getSetShardings.RemoveShardingEntry(databaseInfoName);
        }

        return status;
    }
}