// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ExamplesCommonCode.IdentityCookieCode;

public static class RefreshClaimsExtensions
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
        return new Claim(TimeToRefreshUserClaimType, DateTime.UtcNow.Add(timeTillNextRefresh).ToString("O"));
    }

    /// <summary>
    /// This returns the TimeToRefreshUserClaims as an UTC DataTime. If there is no TimeToRefreshUserClaims claim
    /// then it returns <see cref="DateTime.MinValue"/>
    /// </summary>
    /// <param name="usersClaims">A list of the claims found in the current principal user</param>
    /// <returns></returns>
    public static DateTime GetTimeToRefreshUserValue(this List<Claim> usersClaims)
    {
        var refreshTimeString = usersClaims.FirstOrDefault(x => x.Type == RefreshClaimsExtensions.TimeToRefreshUserClaimType)?.Value;

        return refreshTimeString == null
            ? DateTime.MinValue
            : DateTime.SpecifyKind(DateTime.Parse(refreshTimeString), DateTimeKind.Utc);
    }


}