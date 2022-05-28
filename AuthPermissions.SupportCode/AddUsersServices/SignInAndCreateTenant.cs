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
        IAuthenticationAddUserManager addUserManager, IGetDatabaseForNewTenant getShardingDb = null)
    {
        _options = options;
        _tenantAdmin = tenantAdmin;
        _addUserManager = addUserManager;
        _getShardingDb = getShardingDb;
    }

    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant,
    /// where there is only version of the tenant. It also creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="userInfo">This contains the information for the new user. Both the user login and any AuthP Roles, etc.</param>
    /// <param name="tenantName">This is the name for the new tenant - it will check the name is not already used</param>
    /// <param name="hasOwnDb">If the app is sharding, then must be set to true of tenant having its own db, of false for shared db</param>
    /// <param name="region">Optional: This is used when you have database servers geographically spread.
    /// It helps the <see cref="IGetDatabaseForNewTenant"/> service to pick the right server/database.</param>
    /// <returns>status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric> SignUpNewTenantAsync(AddUserDataDto userInfo, string tenantName, bool? hasOwnDb = null, string region = null)
    {
        var signUpInfo = new AddNewTenantDto
        {
            NewUserInfo = userInfo,
            TenantName = tenantName,
            HasOwnDb = hasOwnDb,
            Region = region,
        };

        return await SignUpNewTenantWithVersionAsync(signUpInfo, new MultiTenantVersionData());
    }

    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant, with versioning.
    /// This method creates the tenant using the <see cref="MultiTenantVersionData"/> for this application
    /// with backup version information provides by the user.
    /// At the same time is creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="signUpInfo">The data provided by the user and extra data, like the version, from the sign in</param>
    /// <param name="versionData">This contains the application's setup of your tenants, including different versions.</param>
    /// <returns>Status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric> SignUpNewTenantWithVersionAsync(AddNewTenantDto signUpInfo, MultiTenantVersionData versionData)
    {
        if (signUpInfo == null) throw new ArgumentNullException(nameof(signUpInfo));
        if (versionData == null) throw new ArgumentNullException(nameof(versionData));
        var status = new StatusGenericHandler();

        if (signUpInfo.TenantName == null)
            return status.AddError("You forgot to give a tenant name");

        //Check if tenant name is available
        if (await _tenantAdmin.QueryTenants().AnyAsync(x => x.TenantFullName == signUpInfo.TenantName))
            return status.AddError($"The tenant name '{signUpInfo.TenantName}' is already taken", new[] { nameof(AddNewTenantDto.TenantName) });

        if (status.CombineStatuses(await _addUserManager.CheckNoExistingAuthUserAsync(signUpInfo.NewUserInfo)).HasErrors)
            return status;

        //---------------------------------------------------------------
        // Create tenant section

        bool? hasOwnDb = null;
        string databaseInfoName = null;
        if (_options.TenantType.IsSharding())
        {
            if (_getShardingDb == null)
                throw new AuthPermissionsException(
                    $"If you are using sharding, then you must register the {nameof(IGetDatabaseForNewTenant)} service.");

            hasOwnDb = GetDirectoryWithChecks(signUpInfo.Version, versionData.HasOwnDbForEachVersion,
                nameof(MultiTenantVersionData.HasOwnDbForEachVersion)) ?? signUpInfo.HasOwnDb;

            if (hasOwnDb == null)
                return status.AddError($"You must set the {nameof(AddNewTenantDto.HasOwnDb)} parameter to true or false");

            //This method will find a database for the new tenant when using sharding
            var dbStatus = await _getShardingDb.FindBestDatabaseInfoNameAsync((bool)hasOwnDb, signUpInfo.Region);
            if (status.CombineStatuses(dbStatus).HasErrors)
                return status;
            databaseInfoName = dbStatus.Result;
        }

        var tenantRoles = GetDirectoryWithChecks(signUpInfo.Version, versionData.TenantRolesForEachVersion,
            nameof(MultiTenantVersionData.TenantRolesForEachVersion));

        var tenantStatus = _options.TenantType.IsSingleLevel()
            ? await _tenantAdmin.AddSingleTenantAsync(signUpInfo.TenantName, tenantRoles,
                hasOwnDb, databaseInfoName)
            : await _tenantAdmin.AddHierarchicalTenantAsync(signUpInfo.TenantName, 0,
                tenantRoles, hasOwnDb, databaseInfoName);

        if (status.CombineStatuses(tenantStatus).HasErrors)
            return status;

        //-------------------------------------------------------------

        //Now we update the sign in user information with the version data
        
        if (signUpInfo.Version != null)
            //Only override the new user's Roles if you are using versioning
            signUpInfo.NewUserInfo.Roles = GetDirectoryWithChecks(signUpInfo.Version, versionData.TenantAdminRoles,
                nameof(MultiTenantVersionData.TenantAdminRoles));
        signUpInfo.NewUserInfo.TenantId = tenantStatus.Result.TenantId;

        status.CombineStatuses(await _addUserManager.SetUserInfoAsync(signUpInfo.NewUserInfo, signUpInfo.NewUserInfo.Password));

        if (status.IsValid)
            status.CombineStatuses(await _addUserManager.LoginVerificationAsync(signUpInfo.NewUserInfo.Email, signUpInfo.NewUserInfo.UserName, signUpInfo.NewUserInfo.IsPersistent));

        if (status.HasErrors)
            //Delete the tenant if anything went wrong, because the user most likely will want to try again
            await _tenantAdmin.DeleteTenantAsync(tenantStatus.Result.TenantId);

        status.Message =
            $"Successfully created the tenant '{tenantStatus.Result.TenantFullName}' and registered you as the tenant admin.";

        return status;
    }

    //---------------------------------------------------
    // private methods

    private T GetDirectoryWithChecks<T>(string versionString, Dictionary<string, T> versionDirectory, string nameOfVersionData)
    {
        if (versionString == null && versionDirectory == null)
            //not using versioning, so send back default value
            return default(T);

        if (versionString == null && versionDirectory != null)
            throw new AuthPermissionsException(string.Format("The version string was null, but {0}.{1} contained version data.",
                nameof(MultiTenantVersionData), nameOfVersionData));

        if (versionString != null && versionDirectory == null)
            throw new AuthPermissionsException(string.Format("You provided a version string, but the {0}.{1} was null.",
                nameof(MultiTenantVersionData), nameOfVersionData));

        if (!versionDirectory.ContainsKey(versionString))
            throw new AuthPermissionsException(string.Format("The version {0} the {1}.{2} was null.",
                versionString, nameof(MultiTenantVersionData), nameOfVersionData));

        return versionDirectory[versionString];
    }
}