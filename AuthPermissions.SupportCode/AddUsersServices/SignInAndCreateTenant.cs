// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using AuthPermissions.SupportCode.ShardingServices;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This class implements the AuthP "sign up" feature, which allows a new user to automatically
/// create a new tenant and becomes the tenant admin user for this new tenant.
/// This class handles different versions, as defined in the <see cref="MultiTenantVersionData"/> class
/// </summary>
public class SignInAndCreateTenant
{
    private readonly AuthPermissionsOptions _options;
    private readonly IAuthTenantAdminService _tenantAdmin;
    private readonly IAuthenticationAddUserManager _addUserManager;
    private readonly IGetDatabaseForNewTenant _getShardingDb;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="tenantAdmin"></param>
    /// <param name="addUserManager"></param>
    /// <param name="getShardingDb"></param>
    public SignInAndCreateTenant(AuthPermissionsOptions options, IAuthTenantAdminService tenantAdmin, 
        IAuthenticationAddUserManager addUserManager, IGetDatabaseForNewTenant getShardingDb)
    {
        _options = options;
        _tenantAdmin = tenantAdmin;
        _addUserManager = addUserManager;
        _getShardingDb = getShardingDb;
    }

    /// <summary>
    /// This implements "sign up" feature, where a new user 
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="versionData"></param>
    /// <param name="password"></param>
    /// <param name="isPersistent"></param>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric> AddUserAndNewTenantAsync(AddNewTenantDto dto, 
        MultiTenantVersionData versionData, string password, bool isPersistent = false)
    {
        var status = new StatusGenericHandler();

        //Check if tenant name is available
        if (await _tenantAdmin.QueryTenants().AnyAsync(x => x.TenantFullName == dto.TenantName))
            return status.AddError($"The tenant name '{dto.TenantName}' is already taken", new[] { nameof(AddNewTenantDto.TenantName) });

        if (versionData.TenantRolesForEachVersion.ContainsKey(dto.Version))
            throw new AuthPermissionsException($"The Version string wasn't found in the {nameof(MultiTenantVersionData.TenantRolesForEachVersion)}");
        if (versionData.HasOwnDbForEachVersion != null && versionData.HasOwnDbForEachVersion.ContainsKey(dto.Version))
            throw new AuthPermissionsException($"The Version string wasn't found in the {nameof(MultiTenantVersionData.HasOwnDbForEachVersion)}");

        if (status.CombineStatuses(await _addUserManager.CheckNoExistingAuthUser(dto)).HasErrors)
            return status;

        //---------------------------------------------------------------
        // Create tenant section

        var hasOwnDb = versionData.HasOwnDbForEachVersion?[dto.Version] ?? dto.HasOwnDb ?? false;

        string databaseInfoName = null;
        if (_options.TenantType.IsSharding())
        {
            //This method will find a database for the new tenant when using sharding
            var dbStatus = await _getShardingDb.FindBestDatabaseInfoNameAsync(hasOwnDb);
            if (status.CombineStatuses(dbStatus).HasErrors)
                return status;
            databaseInfoName = dbStatus.Result;
        }

        var tenantStatus = _options.TenantType.IsSingleLevel()
            ? await _tenantAdmin.AddSingleTenantAsync(dto.TenantName,
                versionData.TenantRolesForEachVersion[dto.Version], dto.HasOwnDb, databaseInfoName)
            : await _tenantAdmin.AddHierarchicalTenantAsync(dto.TenantName, dto.ParentId,
                versionData.TenantRolesForEachVersion[dto.Version], dto.HasOwnDb, databaseInfoName);

        if (status.CombineStatuses(tenantStatus).HasErrors)
            return status;

        //-------------------------------------------------------------

        //Now we update the user information with the version data
        dto.Roles = versionData.TenantAdminRoles;
        dto.TenantId = tenantStatus.Result.TenantId;

        status.CombineStatuses(await _addUserManager.SetUserInfoAsync(dto, password));

        if (status.IsValid)
            status.CombineStatuses(await _addUserManager.LoginVerificationAsync(dto.Email, dto.UserName, isPersistent));

        if (status.HasErrors)
            //Delete the tenant if anything went wrong, because the user most likely will want to try again
            await _tenantAdmin.DeleteTenantAsync(tenantStatus.Result.TenantId);

        status.Message =
            $"Successfully created the tenant '{tenantStatus.Result.TenantFullName}' and registered you as the tenant admin.";

        return status;
    }
}