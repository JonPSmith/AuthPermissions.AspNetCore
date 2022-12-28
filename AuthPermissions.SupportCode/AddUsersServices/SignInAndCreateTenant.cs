// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using AuthPermissions.SupportCode.ShardingServices;
using LocalizeMessagesAndErrors;
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
    private readonly IAddNewUserManager _addNewUserManager;
    private readonly IDefaultLocalizer _localizeDefault;
    private readonly IGetDatabaseForNewTenant _getShardingDb;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="tenantAdmin"></param>
    /// <param name="addNewUserManager"></param>
    /// <param name="localizeProvider"></param>
    /// <param name="getShardingDb"></param>
    public SignInAndCreateTenant(AuthPermissionsOptions options, IAuthTenantAdminService tenantAdmin, 
        IAddNewUserManager addNewUserManager, IAuthPDefaultLocalizer localizeProvider,
        IGetDatabaseForNewTenant getShardingDb = null)
    {
        _options = options;
        _tenantAdmin = tenantAdmin;
        _addNewUserManager = addNewUserManager;
        _localizeDefault = localizeProvider.DefaultLocalizer;
        _getShardingDb = getShardingDb;
    }

    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant,
    /// where there is only version of the tenant. It also creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="newUser">The information for the new user that is signing in.
    /// NOTE: any Roles for the user should be added to the <see cref="AddNewUserDto.Roles"/> property</param>
    /// <param name="tenantData">The information for how the new tenant should be created.
    /// NOTE: Set the <see cref="AddNewTenantDto.HasOwnDb"/> to true/false if sharding is on.</param>
    /// <returns>status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric> SignUpNewTenantAsync(AddNewUserDto newUser, AddNewTenantDto tenantData)
    {
        if (tenantData == null) throw new ArgumentNullException(nameof(tenantData));
        if (tenantData.Version != null)
            throw new AuthPermissionsException(
                $"The {nameof(AddNewTenantDto.Version)} wasn't null. " +
                $"If you want to use versioning then call the {nameof(SignUpNewTenantWithVersionAsync)} method.");

        return await SignUpNewTenantWithVersionAsync(newUser, tenantData, new MultiTenantVersionData());
    }

    /// <summary>
    /// This implements "sign up" feature, where a new user signs up for a new tenant, with versioning.
    /// This method creates the tenant using the <see cref="MultiTenantVersionData"/> for this application
    /// with backup version information provides by the user.
    /// At the same time is creates a new user which is linked to the new tenant.
    /// </summary>
    /// <param name="newUser">The information for the new user that is signing in</param>
    /// <param name="tenantData">The information for how the new tenant should be created</param>
    /// <param name="versionData">This contains the application's setup of your tenants, including different versions.</param>
    /// <returns>Status</returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric<AddNewUserDto>> SignUpNewTenantWithVersionAsync(AddNewUserDto newUser, 
        AddNewTenantDto tenantData, MultiTenantVersionData versionData)
    {
        if (newUser == null) throw new ArgumentNullException(nameof(newUser));
        if (tenantData == null) throw new ArgumentNullException(nameof(tenantData));
        if (versionData == null) throw new ArgumentNullException(nameof(versionData));
        var status = new StatusGenericLocalizer<AddNewUserDto>(_localizeDefault);

        if (tenantData.TenantName == null)
            return status.AddErrorString("NullTenantName".ClassLocalizeKey(this, true),
                "You forgot to give a tenant name.",
                nameof(AddNewTenantDto.TenantName));

        //Check if tenant name is available
        if (await _tenantAdmin.QueryTenants().AnyAsync(x => x.TenantFullName == tenantData.TenantName))
            return status.AddErrorFormattedWithParams("DuplicateTenantName".ClassLocalizeKey(this, true),
                $"The tenant name '{tenantData.TenantName}' is already taken.",
                nameof(AddNewTenantDto.TenantName));

        if (status.CombineStatuses(await _addNewUserManager.CheckNoExistingAuthUserAsync(newUser)).HasErrors)
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

            hasOwnDb = GetDirectoryWithChecks(tenantData.Version, versionData.HasOwnDbForEachVersion,
                nameof(MultiTenantVersionData.HasOwnDbForEachVersion)) ?? tenantData.HasOwnDb;

            if (hasOwnDb == null)
                return status.AddErrorString("HasOwnDbNotSet".ClassLocalizeKey(this, true),
                    $"You must set the {nameof(AddNewTenantDto.HasOwnDb)} parameter to true or false.",
                    nameof(AddNewTenantDto.HasOwnDb));

            //This method will find a database for the new tenant when using sharding
            var dbStatus = await _getShardingDb.FindBestDatabaseInfoNameAsync((bool)hasOwnDb, tenantData.Region, tenantData.Version);
            if (status.CombineStatuses(dbStatus).HasErrors)
                return status;
            databaseInfoName = dbStatus.Result;
        }

        var tenantRoles = GetDirectoryWithChecks(tenantData.Version, 
            versionData.TenantRolesForEachVersion,
            nameof(MultiTenantVersionData.TenantRolesForEachVersion)) ?? new List<string>();

        var tenantStatus = _options.TenantType.IsSingleLevel()
            ? await _tenantAdmin.AddSingleTenantAsync(tenantData.TenantName, tenantRoles,
                hasOwnDb, databaseInfoName)
            : await _tenantAdmin.AddHierarchicalTenantAsync(tenantData.TenantName, 0,
                tenantRoles, hasOwnDb, databaseInfoName);

        if (status.CombineStatuses(tenantStatus).HasErrors)
            return status;

        //-------------------------------------------------------------

        //Now we update the sign in user information with the version data

        try
        {
            //we do this within a try / catch so that if the set up of the user fails the tenant is deleted 

            if (tenantData.Version != null)
                //Only override the new user's Roles if you are using versioning
                newUser.Roles = GetDirectoryWithChecks(tenantData.Version, versionData.TenantAdminRoles,
                    nameof(MultiTenantVersionData.TenantAdminRoles));
            newUser.TenantId = tenantStatus.Result.TenantId;
        
            //From the point where the tenant is created any errors will delete the tenant, as the user might try again

            status.CombineStatuses(await _addNewUserManager.SetUserInfoAsync(newUser));

            if (status.IsValid)
            {
                var loginStatus = await _addNewUserManager.LoginAsync();
                status.CombineStatuses(loginStatus);
                //The LoginAsync returns the final AddNewUserDto for the new user
                //We return this because the UserManager may have altered the data, e.g. the Azure AD manager will create a temporary password 
                status.SetResult(loginStatus.Result); 
            }
        }
        catch
        {
            //Delete the tenant before throwing the exception
            await _tenantAdmin.DeleteTenantAsync(tenantStatus.Result.TenantId);
            throw;
        }

        if (status.HasErrors)
        {
            //Delete the tenant if anything went wrong, because the user most likely will want to try again
            await _tenantAdmin.DeleteTenantAsync(tenantStatus.Result.TenantId);
            return status;
        }

        status.SetMessageFormatted("SuccessSignUp".ClassLocalizeKey(this, true),
            $"Successfully created the tenant '{tenantStatus.Result.TenantFullName}' and registered you as the tenant admin.");

        return status;
    }

    //---------------------------------------------------
    // private methods

    private T GetDirectoryWithChecks<T>(string versionString, Dictionary<string, T> versionDirectory, string nameOfVersionData)
    {
        if (versionString == null && versionDirectory == null)
            //not using versioning, so send back default value
            return default;

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