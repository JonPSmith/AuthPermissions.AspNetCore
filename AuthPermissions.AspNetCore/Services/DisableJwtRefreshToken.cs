// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.AspNetCore.Services
{
    /// <summary>
    /// This service allows you to mark the Jwt Refresh Token as 'used' so that the JWT token cannot be refreshed
    /// </summary>
    public class DisableJwtRefreshToken : IDisableJwtRefreshToken
    {
        private readonly AuthPermissionsDbContext _context;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        public DisableJwtRefreshToken(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This will mark the specified (if empty the latest), valid RefreshToken as invalid.
        /// Call this a) when a user logs out, or b) you want to log out an active user when the JWT times out
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="refreshTokenToDisable"></param>
        public async Task MarkJwtRefreshTokenAsUsedAsync(string userId, string refreshTokenToDisable = null)
        {
            RefreshToken refreshToken = null;

            if (string.IsNullOrWhiteSpace(refreshTokenToDisable))
            {
                // Get the latest refresh token
                refreshToken = await _context.RefreshTokens
                    .Where(x => x.UserId == userId && !x.IsInvalid)
                    .OrderByDescending(x => x.AddedDateUtc)
                    .FirstOrDefaultAsync();
            }
            else
            {
                refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.TokenValue == refreshTokenToDisable);
            }

            if (refreshToken == null)
            {
                return;
            }

            refreshToken.MarkAsInvalid();
            var status = await _context.SaveChangesWithChecksAsync();
            status.IfErrorsTurnToException();
        }
    }
}