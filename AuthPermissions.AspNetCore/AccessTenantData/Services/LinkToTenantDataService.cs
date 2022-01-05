// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthPermissions.AspNetCore.AccessTenantData.Services;

/// <summary>
/// This service defines the admin command to implement the "Access the data of another tenant user" feature - see issue #10
/// It handles the creating, accessing and removing a cookie that carries the DataKey and Name of the tenant to want to access
/// </summary>
public class LinkToTenantDataService : ILinkToTenantDataService
{
    private readonly AuthPermissionsDbContext _context;
    private readonly LinkToTenantTypes _linkToTenantType;
    private readonly IAccessTenantDataCookie _cookieAccessor;
    private readonly AccessTenantDataOptions _cookieOptions;

    private readonly EncryptDecrypt _encryptor;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="cookieAccessor"></param>
    /// <param name="context"></param>
    /// <param name="options"></param>
    /// <param name="dataFromAppSettings"></param>
    public LinkToTenantDataService(IAccessTenantDataCookie cookieAccessor, 
        AuthPermissionsDbContext context,
        AuthPermissionsOptions options,
        IOptions<AccessTenantDataOptions> dataFromAppSettings)
    {
        _context = context;
        _linkToTenantType = options.LinkToTenantType;
        _cookieAccessor = cookieAccessor;
        _cookieOptions = dataFromAppSettings.Value;

        _encryptor = new EncryptDecrypt(_cookieOptions.EncryptionKey);
    }

    /// <summary>
    /// This will change the DataKey to a different tenant than the current user's DataKey
    /// This does this by creating a cookie that contains a DataKey that will replace the current user's DataKey claim
    /// </summary>
    /// <param name="currentUserId">Id of the current user. Used to check that user type matches the </param>
    /// <param name="tenantId">The primary key of the Tenant the user wants to access</param>
    /// <returns></returns>
    /// <exception cref="AuthPermissionsException"></exception>
    public async Task StartLinkingToTenantDataAsync(string currentUserId, int tenantId)
    {
        await CheckUserAgainstLinkToTenantType(currentUserId);

        var tenantToLinkTo = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantId == tenantId);
        if (tenantToLinkTo == null)
            throw new AuthPermissionsException("Could not find the tenant you were looking for.");

        _cookieAccessor.AddOrUpdateCookie(EncodeCookieContent(tenantToLinkTo), _cookieOptions.NumMinuteBeforeCookieTimesOut);
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
    public string GetDataKeyOfLinkedTenant()
    {
        var cookieValue = _cookieAccessor.GetValue();

        return cookieValue == null ? null : DecodeCookieContent(cookieValue).dataKey;
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
        //thanks to https://stackoverflow.com/questions/13254211/how-to-convert-string-to-datetime-as-utc-as-simple-as-that
        //var threeValues = $"{tenantToLinkToTenant.GetTenantDataKey()},{DateTime.UtcNow.ToShortTimeString()},{tenantToLinkToTenant.TenantFullName}";
        var twoValues = $"{tenantToLinkToTenant.GetTenantDataKey()},{tenantToLinkToTenant.TenantFullName}";

        return _encryptor.Encrypt(twoValues);
    }

    private  (string dataKey, string tenantName) DecodeCookieContent(string cookieValue)
    {
        string twoValues;
        try
        {
            twoValues = _encryptor.Decrypt(cookieValue);
        }
        catch
        {
            throw new AuthPermissionsException("The content of the Access Tenant Data cookie was bad.");
        }

        var firstComma = twoValues.IndexOf(',');
        if (firstComma == -1)
            throw new AuthPermissionsException("Could not find the user you were looking for.");

        return (twoValues.Substring(0, firstComma), twoValues.Substring(firstComma + 1));
    }

    private async Task CheckUserAgainstLinkToTenantType(string currentUserId)
    {
        if (_linkToTenantType == LinkToTenantTypes.NotTurnedOn)
            throw new AuthPermissionsException(
                $"You must set up the {nameof(AuthPermissionsOptions.LinkToTenantType)} to use the Access Tenant Data feature.");

        var user = await _context.AuthUsers.SingleOrDefaultAsync(x => x.UserId == currentUserId);
        if (user == null)
            throw new AuthPermissionsException("Could not find the user you were looking for.");

        if (user.TenantId != null && _linkToTenantType != LinkToTenantTypes.AppAndHierarchicalUsers)
            throw new AuthPermissionsException(
                $"The option's {nameof(AuthPermissionsOptions.LinkToTenantType)} parameter is set to {LinkToTenantTypes.OnlyAppUsers}, " +
                $"which means a user linked to a tenant can't use the Access Tenant Data feature.");
    }
}