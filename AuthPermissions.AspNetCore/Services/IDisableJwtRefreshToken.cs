﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.Services
{
    /// <summary>
    /// Service to disable the current JWT Refresh Token
    /// </summary>
    public interface IDisableJwtRefreshToken
    {
        /// <summary>
        /// This will mark the specified (if empty the latest), valid RefreshToken as invalid.
        /// Call this a) when a user logs out, or b) you want to log out an active user when the JWT times out
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="refreshTokenToDisable"></param>
        Task MarkJwtRefreshTokenAsUsedAsync(string userId, string refreshTokenToDisable = null);
    }
}