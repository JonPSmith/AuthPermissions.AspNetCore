// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.SetupCode;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices
{
    /// <summary>
    /// Bulk load AuthUsers 
    /// </summary>
    public interface IBulkLoadUsersService
    {
        /// <summary>
        /// This allows you to add a series of users with their roles and the tenant (if <see cref="AuthPermissions.AuthPermissionsOptions.TenantType"/> says tenants are used
        /// </summary>
        /// <param name="userDefinitions">A list of <see cref="BulkLoadUserWithRolesTenant"/> containing the information on users and what auth roles they have.
        /// In this case the UserId must be filled in with the authorized users' UserId, or the <see cref="IFindUserInfoService"/> can find a user's ID
        /// </param>
        /// <returns>A status so that errors can be returned</returns>
        Task<IStatusGeneric> AddUsersRolesToDatabaseAsync(List<BulkLoadUserWithRolesTenant> userDefinitions);
    }
}