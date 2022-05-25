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
public class SignInAndCreateTenant : ISignInAndCreateTenant
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
    /// This implements "sign up" feature, where a new user signs up for a new tenant.
    /// This method creates the tenant using the information provides by the user and the
    /// <see cref="MultiTenantVersionData"/> for this application.
    /// </summary>
    /// <param name="dto">The data provided by the user and extra data, like the version, from the sign in</param>
    /// <param name="versionData">This contains the application's setup of your tenants, including different versions.</param>
    /// <returns>Status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric> AddUserAndNewTenantAsync(AddNewTenantDto dto, MultiTenantVersionData versionData)
    {
        var status = new StatusGenericHandler();

        //Check if tenant name is available
        if (await _tenantAdmin.QueryTenants().AnyAsync(x => x.TenantFullName == dto.TenantName))
            return status.AddError($"The tenant name '{dto.TenantName}' is already taken", new[] { nameof(AddNewTenantDto.TenantName) });

        if (versionData.TenantRolesForEachVersion != null && versionData.TenantRolesForEachVersion.ContainsKey(dto.Version))
            throw new AuthPermissionsException(string.Format("The Version string {0} wasn't found in the {1}",
                dto.Version ?? "<null>", nameof(MultiTenantVersionData.TenantRolesForEachVersion)));

        if (status.CombineStatuses(await _addUserManager.CheckNoExistingAuthUser(dto)).HasErrors)
            return status;

        //---------------------------------------------------------------
        // Create tenant section

        bool? hasOwnDb = null;
        string databaseInfoName = null;
        if (_options.TenantType.IsSharding())
        {
            if (versionData.HasOwnDbForEachVersion != null && versionData.HasOwnDbForEachVersion.ContainsKey(dto.Version))
                throw new AuthPermissionsException(string.Format("The Version string wasn't found in the {0}",
                    nameof(MultiTenantVersionData.HasOwnDbForEachVersion)));

            hasOwnDb = versionData.HasOwnDbForEachVersion?[dto.Version] ?? dto.HasOwnDb;
            if (hasOwnDb == null)
                return status.AddError($"You must set the {nameof(AddNewTenantDto.HasOwnDb)} parameter to true or false");

            //This method will find a database for the new tenant when using sharding
            var dbStatus = await _getShardingDb.FindBestDatabaseInfoNameAsync((bool)hasOwnDb, dto.Region);
            if (status.CombineStatuses(dbStatus).HasErrors)
                return status;
            databaseInfoName = dbStatus.Result;
        }

        var tenantStatus = _options.TenantType.IsSingleLevel()
            ? await _tenantAdmin.AddSingleTenantAsync(dto.TenantName,
                versionData.TenantRolesForEachVersion?[dto.Version] ?? new List<string>(),
                hasOwnDb, databaseInfoName)
            : await _tenantAdmin.AddHierarchicalTenantAsync(dto.TenantName, 0,
                versionData.TenantRolesForEachVersion?[dto.Version] ?? new List<string>(), 
                hasOwnDb, databaseInfoName);

        if (status.CombineStatuses(tenantStatus).HasErrors)
            return status;

        //-------------------------------------------------------------

        //Now we update the user information with the version data
        if (versionData.TenantAdminRoles != null && versionData.TenantAdminRoles.ContainsKey(dto.Version))
            throw new AuthPermissionsException(string.Format("The Version string {0} wasn't found in the {1}",
                dto.Version ?? "<null>", nameof(MultiTenantVersionData.TenantAdminRoles)));

        dto.Roles = versionData.TenantAdminRoles?[dto.Version] ?? new List<string>();
        dto.TenantId = tenantStatus.Result.TenantId;

        status.CombineStatuses(await _addUserManager.SetUserInfoAsync(dto, dto.Password));

        if (status.IsValid)
            status.CombineStatuses(await _addUserManager.LoginVerificationAsync(dto.Email, dto.UserName, dto.IsPersistent));

        if (status.HasErrors)
            //Delete the tenant if anything went wrong, because the user most likely will want to try again
            await _tenantAdmin.DeleteTenantAsync(tenantStatus.Result.TenantId);

        status.Message =
            $"Successfully created the tenant '{tenantStatus.Result.TenantFullName}' and registered you as the tenant admin.";

        return status;
    }
}