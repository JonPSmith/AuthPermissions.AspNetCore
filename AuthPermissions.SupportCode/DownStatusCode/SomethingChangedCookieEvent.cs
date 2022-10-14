// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This contains the event method that watches for a significant change that effects the user's claims.
/// If a change is found it will compare the time the significant change against the time when
/// the user's claims were last updated. If the user's claims are "older" that the change happens, then their claims are updated
/// </summary>
public static class SomethingChangedCookieEvent
{
    /// <summary>
    /// This is the name of the claim type for a change
    /// </summary>
    public const string EntityChangeClaimType = "EntityChangeClaim";

    /// <summary>
    /// This updates the users claims when a change is registered.
    /// Useful for updating the claims if something is changed
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task UpdateClaimsIfSomethingChangesAsync(CookieValidatePrincipalContext context)
    {
        var originalClaims = context.Principal.Claims.ToList();
        var globalTimeService = context.HttpContext.RequestServices.GetRequiredService<IGlobalChangeTimeService>();
        var lastUpdateUtc = globalTimeService.GetGlobalChangeTimeUtc();

        if (originalClaims.GetClaimDateTimeTicksValue(EntityChangeClaimType) < lastUpdateUtc)
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