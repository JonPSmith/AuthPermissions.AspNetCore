// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This class implements the AuthP "sign up" feature, which allows a new user to 
/// automatically create a new tenant. This class also handles different versions,
/// as defined in the <see cref="MultiTenantVersionData"/> class.
/// </summary>
public class SignInAndCreateTenant : ISignInAndCreateTenant
{
    private readonly AuthPermissionsOptions _options;
    private readonly IAuthTenantAdminService _tenantAdmin;
    private readonly IAddNewUserManager _addNewUserManager;
    private readonly AuthPermissionsDbContext _context;
    private readonly IDefaultLocalizer _localizeDefault;
    private readonly IGetDatabaseForNewTenant _getShardingDb;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="tenantAdmin"></param>
    /// <param name="addNewUserManager"></param>
    /// <param name="context"></param>
    /// <param name="localizeProvider"></param>
    /// <param name="getShardingDb"></param>
    public SignInAndCreateTenant(AuthPermissionsOptions options, IAuthTenantAdminService tenantAdmin,
        IAddNewUserManager addNewUserManager, AuthPermissionsDbContext context,
        IAuthPDefaultLocalizer localizeProvider,
        IGetDatabaseForNewTenant getShardingDb = null)
    {
        _options = options;
        _tenantAdmin = tenantAdmin;
        _addNewUserManager = addNewUserManager;
        _context = context;
        _localizeDefault = localizeProvider.DefaultLocalizer;
        _getShardingDb = getShardingDb;

        if (!_options.TenantType.IsSharding()) return;
        if (_getShardingDb == null)
            throw new AuthPermissionsException(
                $"If you are using sharding, then you must register the {nameof(IGetDatabaseForNewTenant)} service.");
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

        var hasOwnDbStatus = GetHasOwnTypeWithChecks(tenantData, versionData);
        if (status.CombineStatuses(hasOwnDbStatus).HasErrors)
            return status;
        bool hasOwnDb = hasOwnDbStatus.Result;

        var tenantRoles = GetDirectoryWithChecks(tenantData.Version,
            versionData.TenantRolesForEachVersion,
            nameof(MultiTenantVersionData.TenantRolesForEachVersion)) ?? new List<string>();

        //The stages are
        //1. Create the new tenant so that we have its TenantId
        //2. If there is sharding, then
        //   a. Call the FindOrCreateDatabaseAsync to get the database and return the DatabaseInfoName
        //   b. If OK, then update the tenant with the sharding data
        Tenant newTenant = null;
        try
        {
            //NOTE: you mustn't exit this try / catch if you have errors, as we need to "clean up" if there errors

            var tenantStatus = await CreateTenantWithoutShardingAndSave(tenantData, tenantRoles);
            newTenant = tenantStatus.Result;
            
            if (status.CombineStatuses(tenantStatus).IsValid)
            {
                _context.Add(newTenant);
                if (status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault)).IsValid && 
                    _options.TenantType.IsSharding())
                {
                    //This method will find a database for the new tenant when using sharding
                    var dbStatus = await _getShardingDb.FindOrCreateDatabaseAsync(newTenant, 
                        hasOwnDb, tenantData.Region, tenantData.Version);
                    newTenant = dbStatus.Result ?? newTenant;
                    status.CombineStatuses(dbStatus);
                }
            }
        }
        catch (Exception ex)
        {
            status.AddErrorString("ExceptionCreating".ClassMethodLocalizeKey(this, true),
                "Failed to create a new tenant due to an internal error.");
        }

        if (status.HasErrors)
        {
            //We need to undo what was done in the try / catch
            //NOTE: I couldn't use a database transaction because
            // 1. Creating the tenant uses a database transaction, and you can't have a transaction within a transaction
            // 2. The FindOrCreateDatabaseAsync might create a ShardingEntry, which can be in the a json file

            if (newTenant != null)
            {
                if (newTenant.DatabaseInfoName != null)
                    await _getShardingDb.RemoveLastDatabaseSetupAsync();

                await _tenantAdmin.DeleteTenantAsync(newTenant.TenantId);
            }

            return status;
        }

        //-------------------------------------------------------------

        //Now we update the sign in user information with the version data
        AuthUser newAuthUser = null;
        try
        {
            //we do this within a try / catch so that if the set up of the user fails the tenant is deleted 

            if (tenantData.Version != null)
                //Only override the new user's Roles if you are using versioning
                newUser.Roles = GetDirectoryWithChecks(tenantData.Version, versionData.TenantAdminRoles,
                    nameof(MultiTenantVersionData.TenantAdminRoles));
            newUser.TenantId = newTenant.TenantId;
        
            //From the point where the tenant is created any errors will delete the tenant, as the user might try again

            var newAuthUserStatus = await _addNewUserManager.SetUserInfoAsync(newUser);
            newAuthUser = newAuthUserStatus.Result;

            if (status.CombineStatuses(newAuthUserStatus).IsValid)
            {
                var loginStatus = await _addNewUserManager.LoginAsync();
                status.CombineStatuses(loginStatus);
                //The LoginAsync returns the final AddNewUserDto for the new user
                //We return this because the UserManager may have altered the data, e.g. the Azure AD manager will create a temporary password 
                status.SetResult(loginStatus.Result); 
            }
        }
        catch (Exception ex)
        {
            status.AddErrorString("ExceptionUserLogin".ClassMethodLocalizeKey(this, true),
                "Failed to create a new tenant due to an internal error.");
        }

        if (status.HasErrors)
        {
            //Delete the tenant if anything went wrong, because the user most likely will want to try again
            //We need to remove the AuthUser to allow the tenant from be deleted
            if (newAuthUser != null)
                await _addNewUserManager.RemoveAuthUserAsync(newAuthUser.UserId);
            if (newTenant != null)
                await _tenantAdmin.DeleteTenantAsync(newTenant.TenantId);
            if (newTenant?.DatabaseInfoName != null)
                await _getShardingDb.RemoveLastDatabaseSetupAsync();

            return status;
        }

        status.SetMessageFormatted("SuccessSignUp".ClassLocalizeKey(this, true),
            $"Successfully created the tenant '{newTenant.TenantFullName}' and registered you as the tenant admin.");

        return status;
    }

    //---------------------------------------------------
    // private methods

    /// <summary>
    /// This calculates HasOwnDb value, with checks
    /// </summary>
    /// <param name="tenantData"></param>
    /// <param name="versionData"></param>
    /// <returns></returns>
    private IStatusGeneric<bool> GetHasOwnTypeWithChecks(AddNewTenantDto tenantData, MultiTenantVersionData versionData)
    {
        var status = new StatusGenericLocalizer<bool>(_localizeDefault);
        if (!_options.TenantType.IsSharding())
            return status.SetResult(false);

        //We are sharding so run the various tests

        var hasOwnDb = GetDirectoryWithChecks(tenantData.Version, versionData.HasOwnDbForEachVersion,
            nameof(MultiTenantVersionData.HasOwnDbForEachVersion)) ?? tenantData.HasOwnDb;

        if (hasOwnDb == null)
            return status.AddErrorString("HasOwnDbNotSet".ClassLocalizeKey(this, true),
                $"You must set the {nameof(AddNewTenantDto.HasOwnDb)} parameter to true or false.",
                nameof(AddNewTenantDto.HasOwnDb));

        return status.SetResult((bool)hasOwnDb);
    }

    /// <summary>
    /// This creates the Tenant (single or Hierarchical) without any sharding added and saves it to the db
    /// </summary>
    /// <param name="tenantData"></param>
    /// <param name="tenantRoles"></param>
    /// <returns></returns>
    private async Task<IStatusGeneric<Tenant>> CreateTenantWithoutShardingAndSave(AddNewTenantDto tenantData, List<string> tenantRoles)
    {
        var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);

        var tenantRolesStatus = await _tenantAdmin.GetRolesWithChecksAsync(tenantRoles);
        if (status.CombineStatuses(tenantRolesStatus).HasErrors)
            return status;

        status = (StatusGenericLocalizer<Tenant>)(_options.TenantType.IsSingleLevel()
            ? Tenant.CreateSingleTenant(tenantData.TenantName, _localizeDefault, tenantRolesStatus.Result)
            //Note: The added tenant is always a top-level tenant, i.e. it has no parent
            : Tenant.CreateHierarchicalTenant(tenantData.TenantName, null, _localizeDefault, tenantRolesStatus.Result));

        if (status.IsValid)
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

        return status;
    }

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