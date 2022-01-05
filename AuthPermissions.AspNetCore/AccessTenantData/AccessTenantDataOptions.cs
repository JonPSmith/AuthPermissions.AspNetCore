// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AspNetCore.AccessTenantData;

public class AccessTenantDataOptions
{
    /// <summary>
    /// This is a string of at least 16 characters used to encrypt the data in the AccessTenantData cookie
    /// </summary>
    public string EncryptionKey { get; set; } 

    /// <summary>
    /// This provides a time 
    /// </summary>
    public int NumMinuteBeforeCookieTimesOut { get; set; }
}