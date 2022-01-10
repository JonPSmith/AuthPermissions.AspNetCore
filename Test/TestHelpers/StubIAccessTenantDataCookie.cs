// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.AccessTenantData;

namespace Test.TestHelpers;

public class StubIAccessTenantDataCookie : IAccessTenantDataCookie
{
    public string CookieValue { get; set; }

    public void AddOrUpdateCookie(string value, int numMinutesBeforeCookieTimesOut)
    {
        CookieValue = value;
    }

    public bool Exists()
    {
        return CookieValue != null;
    }

    public string GetValue()
    {
        return CookieValue;
    }

    public void DeleteCookie()
    {
        CookieValue = null;
    }
}