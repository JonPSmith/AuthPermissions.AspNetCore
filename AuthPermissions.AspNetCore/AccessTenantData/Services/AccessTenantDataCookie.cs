// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace AuthPermissions.AspNetCore.AccessTenantData.Services;

/// <summary>
/// This is the Cookie used for setting / overriding the DataKey with a different tenant's DataKey
/// </summary>
public class AccessTenantDataCookie : IAccessTenantDataCookie
{
    private const string CookieName = nameof(AccessTenantDataCookie);
    private readonly IRequestCookieCollection _cookiesIn;
    private readonly IResponseCookies _cookiesOut;

    /// <summary>
    /// Takes in the <see cref="IHttpContextAccessor"/> to get the cookie in / out parts
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AccessTenantDataCookie(IHttpContextAccessor httpContextAccessor)
    {
        _cookiesIn = httpContextAccessor.HttpContext?.Request.Cookies;
        _cookiesOut = httpContextAccessor.HttpContext?.Response.Cookies;
    }


    /// <summary>
    /// Add/Update a cookie with the provided string 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="numMinutesBeforeCookieTimesOut">This provides the timeout for the cookie.
    /// This makes sure the change to the DataKey isn't left on too long</param>
    /// <exception cref="NullReferenceException"></exception>
    public void AddOrUpdateCookie(string value, int numMinutesBeforeCookieTimesOut)
    {
        if (_cookiesIn == null) throw new ArgumentNullException(nameof(_cookiesIn));

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.Now.AddMinutes(numMinutesBeforeCookieTimesOut)
        };
        _cookiesOut.Append(CookieName, value, cookieOptions);
    }

    /// <summary>
    /// Returns true if a Cookie exists with the cookieName provided in the ctor 
    /// </summary>
    /// <returns></returns>
    public bool Exists()
    {
        if (_cookiesIn == null) throw new ArgumentNullException(nameof(_cookiesIn));

        return _cookiesIn[CookieName] != null;
    }

    /// <summary>
    /// Returns the value of the string. Can be null if not found or empty
    /// </summary>
    /// <returns></returns>
    public string GetValue()
    {
        if (_cookiesIn == null) throw new ArgumentNullException(nameof(_cookiesIn));

        var cookie = _cookiesIn[CookieName];
        return string.IsNullOrEmpty(cookie) ? null : cookie;
    }

    /// <summary>
    /// Delete the cookie with the cookieName provided in the ctor
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    public void DeleteCookie()
    {
        if (_cookiesOut == null) throw new ArgumentNullException(nameof(_cookiesOut));

        if (!Exists()) return;
        var options = new CookieOptions { Expires = DateTime.UtcNow.AddYears(-1) };
        _cookiesOut.Append(CookieName, "", options);
    }
}