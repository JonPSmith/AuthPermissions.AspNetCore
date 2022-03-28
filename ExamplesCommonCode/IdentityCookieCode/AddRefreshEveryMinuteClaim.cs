// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions;

namespace ExamplesCommonCode.IdentityCookieCode;

public class AddRefreshEveryMinuteClaim : IClaimsAdder
{
    public Task<Claim> AddClaimToUserAsync(string userId)
    {
        var claim = PeriodicCookieEvent.TimeToRefreshUserClaimType
            .CreateClaimDateTimeTicks(new TimeSpan(0, 0, 1, 0));
        return Task.FromResult(claim);
    }
}