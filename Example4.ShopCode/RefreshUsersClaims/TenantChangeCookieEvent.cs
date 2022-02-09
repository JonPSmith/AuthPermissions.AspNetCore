// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.CommonCode;
using ExamplesCommonCode.IdentityCookieCode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Example4.ShopCode.RefreshUsersClaims;

/// <summary>
/// This contains the event method that watches a tenant DataKey changing.
/// If a change is found it will refresh the claims of all the logged-in users
/// </summary>
public static class TenantChangeCookieEvent
{
    /// <summary>
    /// This updates the users claims when an the global change time is newer than the time in the user's
    /// <see cref="RefreshClaimsExtensions.TimeToRefreshUserClaimType"/> claim.
    /// Useful for updating the claims if something is changed
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task UpdateIfGlobalTimeChangedAsync(CookieValidatePrincipalContext context)
    {
        var originalClaims = context.Principal.Claims.ToList();
        var globalAccessor = context.HttpContext.RequestServices.GetRequiredService<IGlobalChangeTimeService>();
        var lastUpdateUtc = globalAccessor.GetGlobalChangeTimeUtc();

        if (originalClaims.GetTimeToRefreshUserValue() < lastUpdateUtc)
        {
            //Need to refresh the user's claims 
            var userId = originalClaims.GetUserIdFromClaims();
            if (userId == null)
                //this shouldn't happen, but best to return
                return;

            var claimsCalculator = context.HttpContext.RequestServices.GetRequiredService<IClaimsCalculator>();
            var newClaims = await claimsCalculator.GetClaimsForAuthUserAsync(userId);
            newClaims.AddRange(originalClaims.RemoveUpdatedClaimsFromOriginalClaims(newClaims)); //Copy over unchanged claims

            var identity = new ClaimsIdentity(newClaims, "Cookie");
            var newPrincipal = new ClaimsPrincipal(identity);
            context.ReplacePrincipal(newPrincipal);
            context.ShouldRenew = true;
        }
    }

    private static IEnumerable<Claim> RemoveUpdatedClaimsFromOriginalClaims(this List<Claim> originalClaims, List<Claim> newClaims)
    {
        var newClaimTypes = newClaims.Select(x => x.Type);
        return originalClaims.Where(x => !newClaimTypes.Contains(x.Type));
    }
}