// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.AspNetCore.Services
{
    /// <summary>
    /// This is a working example of how to send a list of all the user in the Individual Accounts authentication provider
    /// This is used by the <see cref="AuthUsersAdminService.SyncAndShowChangesAsync"/> method which makes sure the AuthP
    /// users are synchronized with users in the Individual Accounts authentication provider
    /// </summary>
    public class SyncIndividualAccountUsers : ISyncAuthenticationUsers
    {
        private readonly UserManager<IdentityUser> _userManager;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="userManager"></param>
        public SyncIndividualAccountUsers(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// This returns the userId, email and UserName of all the users
        /// </summary>
        /// <returns>collection of SyncAuthenticationUser</returns>
        public async Task<IEnumerable<SyncAuthenticationUser>> GetAllActiveUserInfoAsync()
        {
            return await _userManager.Users
                .Select(x => new SyncAuthenticationUser(x.Id, x.Email, x.UserName)).ToListAsync();
        }
    }
}