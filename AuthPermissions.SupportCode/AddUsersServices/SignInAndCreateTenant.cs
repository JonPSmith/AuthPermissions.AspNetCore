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
using Microsoft.Extensions.Logging;
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
    private readonly ILogger _logger;
    private readonly AuthPermissionsDbContext _context;
    private readonly ISignUpGetShardingEntry _getShardingDb;
    private readonly IDefaultLocalizer _localizeDefault;

    //These two fields below contain sharding data. 
    private bool? _hasOwnDb = null;              //default setting says the tenant data is in a shared database
    //This is used in the temp tenant name and in any new ShardingEntries
    private readonly string _createTimestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss-fff");

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="tenantAdmin"></param>
    /// <param name="addNewUserManager"></param>
    /// <param name="localizeProvider"></param>
    /// <param name="logger"></param>
    /// <param name="getShardingDb"></param>
    public SignInAndCreateTenant(AuthPermissionsOptions options, IAuthTenantAdminService tenantAdmin,
        IAddNewUserManager addNewUserManager, IAuthPDefaultLocalizer localizeProvider, 
        ILogger<SignInAndCreateTenant> logger,
        ISignUpGetShardingEntry getShardingDb = null)
    {
        _options = options;
        _tenantAdmin = tenantAdmin;
        _addNewUserManager = addNewUserManager;
        _logger = logger;
        _localizeDefault = localizeProvider.DefaultLocalizer;
        _logger = logger;
        _getShardingDb = getShardingDb;

        if (!_options.TenantType.IsSharding()) return;
        if (_getShardingDb == null)
            throw new AuthPermissionsException(
                $"If you are using sharding, then you must register the {nameof(ISignUpGetShardingEntry)} service.");
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

        //--------------------------------------------------------------
        // Generic tests

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

        //build the tenant and link to a user. We do this within a try / catch to provide the new user to
        //send the timestamp information to the App Admin so that they can find the error. 
        string shardingEntryName = null;    //default setting says that the multi-tenant isn't using sharding

        try
        {
            //---------------------------------------------------------------
            // 1. Handle sharding parts, including getting the Name of ShardingEntry 

            if (_options.TenantType.IsSharding())
            {
                var shardingStatus = await SetupShardingPartsAsync(tenantData, versionData);
                if (status.CombineStatuses(shardingStatus).HasErrors)
                    return status;

                shardingEntryName = shardingStatus.Result;
            }

            //---------------------------------------------------------------
            // 2. Create tenant with a temporary name. This means if its fails that tenant name won't be taken.

            var tenantStatus = await CreateTenantWithTempName(tenantData, versionData, shardingEntryName);
            if (status.CombineStatuses(tenantStatus).HasErrors)
                return status;

            //---------------------------------------------------------------
            // 3. Register the user

            var userStatus = (StatusGenericLocalizer<AddNewUserDto>)await SignInTenantUserAsync(tenantStatus.Result, newUser,
                tenantData, versionData);
            if (status.CombineStatuses(userStatus).HasErrors)
                return status;

            //---------------------------------------------------------------
            // 4.Update the tenant's name to the correct name. This is only done after the 

            var updateStatus =
                await _tenantAdmin.UpdateTenantNameAsync(tenantStatus.Result.TenantId, tenantData.TenantName);

            status.CombineStatuses(updateStatus);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Critical error in SignOn. The timestamp of this Exception is {createTimestamp}.", _createTimestamp);
            status.AddErrorFormatted("ExceptionCreating".ClassMethodLocalizeKey(this, true),
                $"Failed to create a new tenant due to an internal error.",
                $"Contact the support team and provide the string '{_createTimestamp}' to help them fix your problem.");
        }

        status.SetMessageFormatted("SuccessSignUp".ClassLocalizeKey(this, true),
            $"Successfully created the tenant '{tenantData.TenantName}' and registered you as the tenant admin.");

        return status;
    }

    //---------------------------------------------------
    // private methods

    /// <summary>
    /// This is called if the multi-tenant is using sharding. It sets the _HasOwnDb and
    /// finds or creates a <see cref="ShardingEntry"/> for the tenant, which might need
    /// the 
    /// </summary>
    /// <param name="tenantData">Data defined by the user</param>
    /// <param name="versionData">Data defined by the <see cref="MultiTenantVersionData"/> for this application</param>
    /// <returns>status, with the Name of the <see cref="ShardingEntry"/> if no errors</returns>
    private async Task<IStatusGeneric<string>> SetupShardingPartsAsync(AddNewTenantDto tenantData, MultiTenantVersionData versionData)
    {
        var status = new StatusGenericLocalizer<string>(_localizeDefault);
        _hasOwnDb = GetDataFromVersions(tenantData.Version, versionData.HasOwnDbForEachVersion,
            nameof(MultiTenantVersionData.HasOwnDbForEachVersion)) ?? tenantData.HasOwnDb;

        if (_hasOwnDb == null)
            return status.AddErrorString("HasOwnDbNotSet".ClassLocalizeKey(this, true),
                $"You must set the {nameof(AddNewTenantDto.HasOwnDb)} parameter to true or false.",
                nameof(AddNewTenantDto.HasOwnDb));

        //Now we need to get the name of the ShardingEntry providing the data to get to its data 
        //This is done via a developer-written service 
        var shardingStatus = await _getShardingDb.FindOrCreateShardingEntryAsync(
            (bool)_hasOwnDb, _createTimestamp, tenantData.Region, tenantData.Version);

        return shardingStatus;
    }

    /// <summary>
    /// This handles the creation of the Tenant based on the provided tenantData and the versionData.
    /// The new tenant will be created, but the tenant's name will have a temporary name.
    /// This will also organise the <see cref="ShardingEntry"/> if sharding is being used.
    /// </summary>
    /// <param name="tenantData">Data defined by the user</param>
    /// <param name="versionData">Data defined by the <see cref="MultiTenantVersionData"/> for this application</param>
    /// <param name="shardingEntryName">This holds the name of the ShardingEntry if using sharding, or null if not using sharding</param>
    /// <returns></returns>
    private async Task<IStatusGeneric<Tenant>> CreateTenantWithTempName(
        AddNewTenantDto tenantData, MultiTenantVersionData versionData, string shardingEntryName)
    {
        var tenantRoles = GetDataFromVersions(tenantData.Version,
            versionData.TenantRolesForEachVersion,
            nameof(MultiTenantVersionData.TenantRolesForEachVersion)) ?? new List<string>();

        //We create a unique name for the tenant at this stage. Once the sign in has correctly created the
        //tenant and the new user, then the tenant name will be set to the sign-in TenantName
        var tempTenantName = $"TempSignIn-{_createTimestamp}";

        return _options.TenantType.IsSingleLevel()
            ? await _tenantAdmin.AddSingleTenantAsync(tempTenantName, tenantRoles, _hasOwnDb, shardingEntryName)
            //Note: The added tenant is always a top-level tenant, i.e. it has no parent
            : await _tenantAdmin.AddHierarchicalTenantAsync(tempTenantName, 
                0, tenantRoles, _hasOwnDb, shardingEntryName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newTenant"></param>
    /// <param name="newUser"></param>
    /// <param name="tenantData"></param>
    /// <param name="versionData"></param>
    /// <returns></returns>
    private async Task<IStatusGeneric<AddNewUserDto>> SignInTenantUserAsync(Tenant newTenant, AddNewUserDto newUser, 
        AddNewTenantDto tenantData, MultiTenantVersionData versionData)
    {
        var status = new StatusGenericLocalizer<AddNewUserDto>(_localizeDefault);

        if (tenantData.Version != null)
            //Only override the new user's Roles if you are using versioning
            newUser.Roles = GetDataFromVersions(tenantData.Version, versionData.TenantAdminRoles,
                nameof(MultiTenantVersionData.TenantAdminRoles));
        newUser.TenantId = newTenant.TenantId;

        //From the point where the tenant is created any errors will delete the tenant, as the user might try again

        var newAuthUserStatus = await _addNewUserManager.SetUserInfoAsync(newUser);

        if (status.CombineStatuses(newAuthUserStatus).IsValid)
        {
            var loginStatus = await _addNewUserManager.LoginAsync();
            status.CombineStatuses(loginStatus);
            //The LoginAsync returns the final AddNewUserDto for the new user
            //We return this because the UserManager may have altered the data, e.g. the Azure AD manager will create a temporary password 
            status.SetResult(loginStatus.Result);
        }

        return status;
    }

    /// <summary>
    /// This obtains information from the <see cref="MultiTenantVersionData"/>,
    /// or if there is no <see cref="MultiTenantVersionData"/> data, then it returns default
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="versionString"></param>
    /// <param name="versionDirectory"></param>
    /// <param name="nameOfVersionData"></param>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    private T GetDataFromVersions<T>(string versionString, Dictionary<string, T> versionDirectory, string nameOfVersionData)
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