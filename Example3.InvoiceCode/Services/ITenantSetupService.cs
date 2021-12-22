// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Example3.InvoiceCode.Dtos;
using Microsoft.AspNetCore.Identity;
using StatusGeneric;

namespace Example3.InvoiceCode.Services;

public interface ITenantSetupService
{
    /// <summary>
    /// This does three things (with lots of checks)
    /// - Adds the new user to the the individual account
    /// - Adds an AuthUser for this person
    /// - Creates the tenant with the correct tenant roles
    /// </summary>
    /// <param name="dto">The information from the user</param>
    /// <returns>Status</returns>
    Task<IStatusGeneric> CreateNewTenantAsync(CreateTenantDto dto);

    /// <summary>
    /// This creates a an encrypted string containing the tenantId and the user's email
    /// so that you can confirm the user is valid
    /// </summary>
    /// <param name="tenantId">Id of the tenant you want the user to join</param>
    /// <param name="emailOfJoiner">email of the user</param>
    /// <returns>encrypted string to send the user</returns>
    string InviteUserToJoinTenantAsync(int tenantId, string emailOfJoiner);

    /// <summary>
    /// This will take the new user's information plus the encrypted invite code and
    /// 1. decides if the invite matches the user's email
    /// 2. It will create an individual accounts user (if not there), plus a check teh user isn't already an authP user
    /// 3. Then it will create an authP user linked to the tenant they were invited to
    /// NOTE: You MUST sign in the user using the email and password they provided via the individual accounts signInManager
    /// </summary>
    /// <param name="email">email given to log in</param>
    /// <param name="password">password given to log in</param>
    /// <param name="verify">The encrypted part of the url that was created by <see cref="InviteUserToJoinTenantAsync"/></param>
    /// <returns>Status with the individual accounts user</returns>
    Task<IStatusGeneric<IdentityUser>> AcceptUserJoiningATenantAsync(string email, string password, string verify);
}