// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.AspNetCore.Services
{
    /// <summary>
    /// This service allows you to mark the Jwt Refresh Token as 'used' so that the JWT token cannot be refreshed.
    /// There are two methods: one to allow a user to logout and another to allow a admin person log out all logins of a specific user. 
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
        /// This allows a user to logout. The effect is that when the JWT Token expires
        /// you cannot refresh the JWT Token because the refresh token is invalid.
        /// If the user has multiple logins, this only logs out the login using the given refresh token.
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public async Task LogoutUserViaRefreshTokenAsync(string refreshToken)
        {
            var latestValidRefreshToken = await _context.RefreshTokens
                .SingleOrDefaultAsync(x => x.TokenValue == refreshToken);

            if (latestValidRefreshToken != null)
            {
                latestValidRefreshToken.MarkAsInvalid();
                var status = await _context.SaveChangesWithChecksAsync(new StubDefaultLocalizer());
                status.IfErrorsTurnToException();
            }
        }

        /// <summary>
        /// This will mark all the refresh tokens linked to this userid will be marked as invalid,
        /// which means the user cannot refresh the JWT Token. If the user has multiple logins,
        /// then all the users logins will marked as a invalid. 
        /// </summary>
        /// <param name="userId"></param>
        public async Task LogoutUserViaUserIdAsync(string userId)
        {
            var latestValidRefreshTokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsInvalid)
                .ToListAsync();

            latestValidRefreshTokens.ForEach(x => x.MarkAsInvalid());
            var status = await _context.SaveChangesWithChecksAsync(new StubDefaultLocalizer());
            status.IfErrorsTurnToException();
        }
    }
}