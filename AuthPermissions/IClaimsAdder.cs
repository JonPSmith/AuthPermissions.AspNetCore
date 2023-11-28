// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;

namespace AuthPermissions;

/// <summary>
/// This interface defines the way to add a claim to a user on login
/// and on refresh of the user's claims (e.g. when JWT Token refresh happens).
/// </summary>
public interface IClaimsAdder
{
    /// <summary>
    /// This returns a claim to add to the current user. A return of null means don't add a claim
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>A claim, or null if no claim should be added</returns>
    Task<Claim> AddClaimToUserAsync(string userId);
}