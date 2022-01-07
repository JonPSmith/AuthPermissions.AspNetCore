// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.AccessTenantData.Services;

namespace AuthPermissions.AspNetCore.AccessTenantData;

/// <summary>
/// Holds the 
/// </summary>
public class AccessTenantDataOptions
{
    /// <summary>
    /// Use in <see cref="LinkToTenantDataService"/> error message and when configuring
    /// </summary>
    public const string AppSettingsSection = "AccessTenantData";

    /// <summary>
    /// This is a string of at least 16 characters used to encrypt the data in the AccessTenantData cookie
    /// </summary>
    public string EncryptionKey { get; set; }

    /// <summary>
    /// This provides the number of hours that the <see cref="AccessTenantDataCookie"/> will stay around
    /// Defaults to 10 hours.
    /// NOTE: You should call the <see cref="ILinkToTenantDataService.StopLinkingToTenant"/> method when a linked user logs out
    /// </summary>
    public int NumHoursBeforeCookieTimesOut { get; set; } = 10;
}