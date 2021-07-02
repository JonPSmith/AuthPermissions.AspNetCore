// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.Services
{
    public interface IDisableJwtRefreshToken
    {
        /// <summary>
        /// This will mark the latest, valid RefreshToken as invalid.
        /// Call this a) when a user logs out, or b) you want to log out an active user when the JTW times out
        /// </summary>
        /// <param name="userId"></param>
        Task MarkJwtRefreshTokenAsUsedAsync(string userId);
    }
}