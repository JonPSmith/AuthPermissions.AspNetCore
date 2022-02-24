// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ExamplesCommonCode.IdentityCookieCode;

public static class TimeClaimExtensions
{

    /// <summary>
    /// Used in the "periodically update user's claims" feature
    /// </summary>
    public const string TimeToRefreshUserClaimType = "TimeToRefreshUserClaims";

    /// <summary>
    /// This creates the TimeToRefreshUserClaims claim containing the UTC time when the user's claims should be recalculated
    /// </summary>
    /// <param name="timeTillNextRefresh">This is the timespan to add on to the current UtcNow to define the time when
    /// the user's claims should be refreshed</param>
    /// <returns></returns>
    public static Claim CreateTimeToRefreshUserClaim(this TimeSpan timeTillNextRefresh)
    {
        return new Claim(TimeToRefreshUserClaimType, DateTime.UtcNow.Add(timeTillNextRefresh).DateTimeToStringUtc());
    }

    /// <summary>
    /// This returns the TimeToRefreshUserClaims as an UTC DataTime. If there is no TimeToRefreshUserClaims claim
    /// then it returns <see cref="DateTime.MinValue"/>
    /// </summary>
    /// <param name="usersClaims">A list of the claims found in the current principal user</param>
    /// <returns></returns>
    public static DateTime GetTimeToRefreshUserValue(this List<Claim> usersClaims)
    {
        var refreshTimeString = usersClaims.FirstOrDefault(x => x.Type == TimeToRefreshUserClaimType)?.Value;
        return refreshTimeString?.StringToDateTimeUtc() ?? DateTime.MinValue;
    }

    /// <summary>
    /// This sets the dateTime to UTC and then turns into a parseable string
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string DateTimeToStringUtc(this DateTime dateTime)
    {
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToString("O");
    }

    /// <summary>
    /// This parses the string into a DateTime and the DateTime is set to UTC
    /// </summary>
    /// <param name="dateTimeString"></param>
    /// <returns></returns>
    public static DateTime StringToDateTimeUtc(this string dateTimeString)
    {
        return DateTime.SpecifyKind(DateTime.Parse(dateTimeString), DateTimeKind.Utc);
    }
}