// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.AccessTenantData.Services;

namespace AuthPermissions.AspNetCore.AccessTenantData;

/// <summary>
/// The methods in the <see cref="AccessTenantDataCookie"/> class
/// </summary>
public interface IAccessTenantDataCookie
{
    /// <summary>
    /// Add/Update a cookie with the provided string 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="numMinutesBeforeCookieTimesOut">This provides the timeout for the cookie.
    /// This makes sure the change to the DataKey isn't left on too long</param>
    /// <exception cref="NullReferenceException"></exception>
    void AddOrUpdateCookie(string value, int numMinutesBeforeCookieTimesOut);

    /// <summary>
    /// Returns true if a Cookie exists with the cookieName provided in the ctor 
    /// </summary>
    /// <returns></returns>
    bool Exists();

    /// <summary>
    /// Returns the value of the string. Can be null if not found or empty
    /// </summary>
    /// <returns></returns>
    string GetValue();

    /// <summary>
    /// Delete the cookie with the cookieName provided in the ctor
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    void DeleteCookie();
}