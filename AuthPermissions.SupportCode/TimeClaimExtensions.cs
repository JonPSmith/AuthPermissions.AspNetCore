// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;

namespace AuthPermissions.SupportCode;

/// <summary>
/// Methods to convert / reads DateTime. Used in the global change code
/// </summary>
public static class TimeClaimExtensions
{
    /// <summary>
    /// This creates a claim containing the UTC time, with a possible offset, as ticks 
    /// </summary>
    /// <param name="claimName"></param>
    /// <param name="offset">This is the timespan to add on to the current UtcNow to define the time when
    ///     the user's claims should be refreshed</param>
    /// <returns></returns>
    public static Claim CreateClaimDateTimeTicks(this string claimName, TimeSpan offset = default)
    {
        return new Claim(claimName, DateTime.UtcNow.Add(offset).Ticks.ToString());
    }

    /// <summary>
    /// This returns the TimeToRefreshUserClaims as an UTC DataTime. If there is no TimeToRefreshUserClaims claim
    /// then it returns <see cref="DateTime.MinValue"/>
    /// </summary>
    /// <param name="usersClaims">A list of the claims found in the current principal user</param>
    /// <param name="claimName"></param>
    /// <returns></returns>
    public static DateTime GetClaimDateTimeTicksValue(this List<Claim> usersClaims, string claimName)
    {
        var timeTicksString = usersClaims.FirstOrDefault(x => x.Type == claimName)?.Value;
        return timeTicksString?.TicksToDateTimeUtc() ?? DateTime.MinValue;
    }

    /// <summary>
    /// This sets the dateTime to UTC and then turns into a parseable string
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string DateTimeToTicks(this DateTime dateTime)
    {
        return dateTime.Ticks.ToString();
    }

    /// <summary>
    /// This parses the string containing ticks into a DateTime and the DateTime is set to UTC
    /// </summary>
    /// <param name="dateTimeTicksString"></param>
    /// <returns></returns>
    public static DateTime TicksToDateTimeUtc(this string dateTimeTicksString)
    {
        var ticks = long.Parse(dateTimeTicksString);
        return DateTime.SpecifyKind(new DateTime(ticks), DateTimeKind.Utc);
    }
}