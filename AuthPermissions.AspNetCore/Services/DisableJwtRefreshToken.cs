// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
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
        /// This will mark the latest, valid RefreshToken as invalid.
        /// Call this a) when a user logs out, or b) you want to log out an active user when the JTW times out
        /// </summary>
        /// <param name="userId"></param>
        public async Task MarkJwtRefreshTokenAsUsedAsync(string userId)
        {
            var latestValidRefreshToken = await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsInvalid)
                .OrderByDescending(x => x.AddedDateUtc)
                .FirstOrDefaultAsync();

            if (latestValidRefreshToken != null)
            {
                latestValidRefreshToken.MarkAsInvalid();
                var status = await _context.SaveChangesWithChecksAsync();
                status.IfErrorsTurnToException();
            }
        }
    }
}