// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.AccessTenantData.Services;

/// <summary>
/// This service defines the admin command to implement the "Access the data of other tenant" feature - see issue #10
/// It handles the creating, accessing and removing a cookie that carries the DataKey and Name of the tenant to want to access
/// </summary>
public class LinkToTenantDataService : ILinkToTenantDataService
{
    private readonly AuthPermissionsDbContext _context;
    private readonly AuthPermissionsOptions _options;
    private readonly IAccessTenantDataCookie _cookieAccessor;

    private readonly IEncryptDecryptService _encryptorService;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="context"></param>
    /// <param name="options"></param>
    /// <param name="cookieAccessor"></param>
    /// <param name="encryptorService"></param>
    /// <param name="localizeProvider"></param>
    public LinkToTenantDataService( 
        AuthPermissionsDbContext context,
        AuthPermissionsOptions options,
        IAccessTenantDataCookie cookieAccessor,
        IEncryptDecryptService encryptorService,
        IAuthPDefaultLocalizer localizeProvider)
    {
        _context = context;
        _options = options;
        _cookieAccessor = cookieAccessor;
        _encryptorService = encryptorService;
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    /// <summary>
    /// This will change the DataKey to a different tenant than the current user's DataKey
    /// This does this by creating a cookie that contains a DataKey that will replace the current user's DataKey claim
    /// </summary>
    /// <param name="currentUserId">Id of the current user. Used to check that user type matches the </param>
    /// <param name="tenantId">The primary key of the Tenant the user wants to access</param>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task<IStatusGeneric> StartLinkingToTenantDataAsync(string currentUserId, int tenantId)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        if (_options.LinkToTenantType == LinkToTenantTypes.NotTurnedOn)
            throw new AuthPermissionsException(
                $"You must set up the {nameof(AuthPermissionsOptions.LinkToTenantType)} to use the Access Tenant Data feature.");

        var user = await _context.AuthUsers.SingleOrDefaultAsync(x => x.UserId == currentUserId);
        if (user == null)
            return status.AddErrorString("UserNotFound".ClassLocalizeKey(this, true), //common 
                "Could not find the user you were looking for.");

        if (user.TenantId != null && _options.LinkToTenantType != LinkToTenantTypes.AppAndHierarchicalUsers)
            throw new AuthPermissionsException(
                $"The option's {nameof(AuthPermissionsOptions.LinkToTenantType)} parameter is set to {LinkToTenantTypes.OnlyAppUsers}, " +
                "which means a user linked to a tenant can't use the Access Tenant Data feature.");

        var tenantToLinkTo = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == tenantId);
        if (tenantToLinkTo == null)
            return status.AddErrorString("TenantNotFound".ClassLocalizeKey(this, true), 
                "Could not find the tenant you were looking for.");

        if (status.HasErrors)
            return status;

        _cookieAccessor.AddOrUpdateCookie(EncodeCookieContent(tenantToLinkTo), _options.NumMinutesBeforeCookieTimesOut);

        status.SetMessageFormatted("Success".ClassLocalizeKey(this, true), 
            $"You are now linked the the data of the tenant called '{tenantToLinkTo.TenantFullName}'");
        return status;
    }

    /// <summary>
    /// This stops the current user's DataKey being set by the <see cref="StartLinkingToTenantDataAsync"/> method.
    /// It simply deletes the <see cref="AccessTenantDataCookie"/>
    /// </summary>
    public void StopLinkingToTenant()
    {
        _cookieAccessor.DeleteCookie();
    }

    /// <summary>
    /// This gets the DataKey from the <see cref="AccessTenantDataCookie"/>
    /// If there no cookie it returns null
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public string GetDataKeyOfLinkedTenant()
    {
        if (_options.TenantType.IsSharding())
            throw new AuthPermissionsException("You shouldn't be using this method if sharding is turn on");

        var cookieValue = _cookieAccessor.GetValue();

        return cookieValue == null ? null : DecodeCookieContent(cookieValue).dataKey;
    }

    /// <summary>
    /// This gets the DataKey and ConnectionName from the <see cref="AccessTenantDataCookie"/>
    /// If there no cookie it returns null for both properties
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public (string dataKey, string connectionName) GetShardingDataOfLinkedTenant()
    {
        if (!_options.TenantType.IsSharding())
            throw new AuthPermissionsException("You shouldn't be using this method if sharding is turned off");

        var cookieValue = _cookieAccessor.GetValue();
        if (cookieValue == null)
            return (null, null);

        var content = DecodeCookieContent(cookieValue);

        return (content.dataKey, content.connectionName);
    }

    /// <summary>
    /// This gets the TenantFullName of the tenant that the <see cref="AccessTenantDataCookie"/> contains
    /// If there no cookie it returns null
    /// </summary>
    /// <returns></returns>
    public string GetNameOfLinkedTenant()
    {
        var cookieValue = _cookieAccessor.GetValue();

        return cookieValue == null ? null : DecodeCookieContent(cookieValue).tenantName;
    }


    //--------------------------------------------------
    // private methods

    private string EncodeCookieContent(Tenant tenantToLinkToTenant)
    {
        var values = _options.TenantType.IsSharding()
            ? $"{tenantToLinkToTenant.GetTenantDataKey()},{tenantToLinkToTenant.DatabaseInfoName},{tenantToLinkToTenant.TenantFullName}"
            : $"{tenantToLinkToTenant.GetTenantDataKey()},{tenantToLinkToTenant.TenantFullName}";

        return _encryptorService.Encrypt(values);
    }

    private  (string dataKey, string tenantName, string connectionName) DecodeCookieContent(string cookieValue)
    {
        string values;
        try
        {
            values = _encryptorService.Decrypt(cookieValue);
        }
        catch
        {
            throw new AuthPermissionsException("The content of the Access Tenant Data cookie was bad.");
        }

        var firstComma = values.IndexOf(',');
        if (firstComma == -1)
            throw new AuthPermissionsException("Could not find the user you were looking for.");

        //without sharding
        if (!_options.TenantType.HasFlag(TenantTypes.AddSharding))
            return (values.Substring(0, firstComma), values.Substring(firstComma + 1), null);

        //with sharding (order is DataKey, ConnectionName, Tenant name - this overcomes the problem of commas in the tenant name
        var secondComma = values.Substring(firstComma + 1).IndexOf(',')+ firstComma + 1;
        return (values.Substring(0, firstComma),
            values.Substring(secondComma + 1),
            values.Substring(firstComma + 1, secondComma - firstComma - 1 ));
    }
}