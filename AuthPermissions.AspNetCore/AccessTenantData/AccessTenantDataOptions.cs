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
    /// This provides the number of minutes that the <see cref="AccessTenantDataCookie"/> will stay around
    /// Defaults to 60 minutes. If set to 0, then it will become a session cookie and stay there all the time
    /// </summary>
    public int NumMinuteBeforeCookieTimesOut { get; set; } = 60;
}