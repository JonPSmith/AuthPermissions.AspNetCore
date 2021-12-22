// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Example3.InvoiceCode.Dtos;
using StatusGeneric;

namespace Example3.InvoiceCode.Services;

public interface ITenantSetupServices
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
    /// This creates a url to send to a user, with an encrypted string containing the tenantId and the user's email
    /// so that you can confirm the user is valid
    /// </summary>
    /// <param name="tenantId">Id of the tenant you want the user to join</param>
    /// <param name="emailOfJoiner">email of the user</param>
    /// <param name="urlToGoTo">url where the user should go to to gain access to the tenant</param>
    /// <returns></returns>
    string InviteUserToJoinTenantAsync(int tenantId, string emailOfJoiner, string urlToGoTo);

    Task<IStatusGeneric> AcceptUserJoiningATenant(string email, string password, string verify);
}