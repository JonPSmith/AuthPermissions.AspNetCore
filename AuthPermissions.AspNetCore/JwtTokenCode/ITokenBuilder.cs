// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AspNetCore.JwtTokenCode
{
     /// <summary>
     /// Interfaces of the JTW Token builder and the refresh token
     /// </summary>
    public interface ITokenBuilder
    {
        /// <summary>
        /// This creates a JWT token containing the claims from the AuthPermissions database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<string> GenerateJwtTokenAsync(string userId);

        /// <summary>
        /// This generates a JWT token containing the claims from the AuthPermissions database
        /// and a Refresh token to go with this token
        /// </summary>
        /// <returns></returns>
        Task<TokenAndRefreshToken> GenerateTokenAndRefreshTokenAsync(string userId);

        /// <summary>
        /// This will refresh the JWT token if the JWT is valid (but can be expired) and the RefreshToken in the database is valid
        /// </summary>
        /// <param name="tokenAndRefresh"></param>
        /// <returns></returns>
        Task<(TokenAndRefreshToken updatedTokens, int HttpStatusCode)> RefreshTokenUsingRefreshTokenAsync(TokenAndRefreshToken tokenAndRefresh);
    }
}