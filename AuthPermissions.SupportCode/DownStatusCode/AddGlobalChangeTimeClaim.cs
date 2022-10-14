// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This adds a claim used with the "update claims on change" feature
/// </summary>
public class AddGlobalChangeTimeClaim : IClaimsAdder
{
    /// <summary>
    /// This adds the current time (utc) as the last time the user's claims were updated
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<Claim> AddClaimToUserAsync(string userId)
    {
        var claim = SomethingChangedCookieEvent.EntityChangeClaimType.CreateClaimDateTimeTicks();
        return Task.FromResult(claim);
    }
}