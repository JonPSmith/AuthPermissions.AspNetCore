// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthPermissions.AspNetCore.JwtTokenCode
{
    /// <summary>
    /// This contains the code to create/refresh JWT tokens
    /// </summary>
    public class TokenBuilder : ITokenBuilder
    {
        private readonly AuthPermissionsOptions _options;
        private readonly IClaimsCalculator _claimsCalculator;
        private readonly AuthPermissionsDbContext _context;
        private readonly ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="claimsCalculator"></param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public TokenBuilder(AuthPermissionsOptions options, 
            IClaimsCalculator claimsCalculator,
            AuthPermissionsDbContext context,
            ILogger<TokenBuilder> logger)
        {
            _options = options;
            _context = context;
            _claimsCalculator = claimsCalculator;
            _logger = logger;
        }

        /// <summary>
        /// This creates a JWT token containing the claims from the AuthPermissions database
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<string> GenerateJwtTokenAsync(string userId)
        {
            var claims = await _claimsCalculator.GetClaimsForAuthUserAsync(userId);
            var tokenAndDesc = GenerateJwtTokenHandler(userId, claims);
            var token = tokenAndDesc.tokenHandler.CreateToken(tokenAndDesc.tokenDescriptor);
            return tokenAndDesc.tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// This generates a JWT token containing the claims from the AuthPermissions database
        /// and a Refresh token to go with this token
        /// </summary>
        /// <returns></returns>
        public async Task<TokenAndRefreshToken> GenerateTokenAndRefreshTokenAsync(string userId)
        {
            if (_options.ConfigureAuthPJwtToken.RefreshTokenExpires == default)
                throw new AuthPermissionsBadDataException(
                    $"The {nameof(AuthPJwtConfiguration)}.{nameof(AuthPJwtConfiguration.RefreshTokenExpires)} must be set with a TimeSpan value.");

            var claims = await _claimsCalculator.GetClaimsForAuthUserAsync(userId);
            var tokenAndDesc = GenerateJwtTokenHandler(userId, claims);
            var token = tokenAndDesc.tokenHandler.CreateToken(tokenAndDesc.tokenDescriptor);

            var refreshToken = RefreshToken.CreateNewRefreshToken(userId, token.Id);
            _context.Add(refreshToken);
            var status = await _context.SaveChangesWithChecksAsync(new StubDefaultLocalizer());
            status.IfErrorsTurnToException();

            return new TokenAndRefreshToken
            {
                Token = tokenAndDesc.tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.TokenValue
            };
        }

        /// <summary>
        /// This will refresh the JWT token if the JWT is valid (but can be expired) and the RefreshToken in the database is valid
        /// </summary>
        /// <param name="tokenAndRefresh"></param>
        /// <returns></returns>
        public async Task<(TokenAndRefreshToken updatedTokens, int HttpStatusCode)> RefreshTokenUsingRefreshTokenAsync(TokenAndRefreshToken tokenAndRefresh)
        {

            var claimsPrincipal = GetPrincipalFromExpiredToken(tokenAndRefresh.Token);
            if (claimsPrincipal == null)
            {
                //The JWT didn't pass the validation - this is a potential problem
                _logger.LogWarning("The token didn't pass the validation. Token = {0}", tokenAndRefresh.Token);
                return (null, 400); //BadRequest
            }

            var refreshTokenFromDb =
                _context.RefreshTokens.SingleOrDefault(x => x.TokenValue == tokenAndRefresh.RefreshToken);
            if (refreshTokenFromDb == null)
            {
                //Could not find the refresh token in the database - this is a potential problem
                _logger.LogWarning("No refresh token was found in the database. Token = {0}", tokenAndRefresh.Token);
                return (null, 400); //BadRequest
            }

            if (refreshTokenFromDb.IsInvalid)
            {
                //Refresh token was a) has already been used, or b) manually  - this is a potential problem
                _logger.LogWarning("The refresh token in the database was marked as {0}. Token = {1}",
                    nameof(refreshTokenFromDb.IsInvalid), tokenAndRefresh.Token);
                return (null, 401); //Unauthorized - need to log in again
            }
            if (refreshTokenFromDb.AddedDateUtc.Add(_options.ConfigureAuthPJwtToken.RefreshTokenExpires) < DateTime.UtcNow)
            {
                //Refresh token was out of date
                var howFarOutOfDate = refreshTokenFromDb.AddedDateUtc.Add(_options.ConfigureAuthPJwtToken.RefreshTokenExpires)
                    .Subtract(DateTime.UtcNow);
                _logger.LogInformation("Refresh token had expired by {0:g}. Token = {1}",
                    howFarOutOfDate, tokenAndRefresh.Token);
                return (null, 401); //Unauthorized - need to log in again
            }

            //Success, so we ...
            //a) get the UserId
            var userId = claimsPrincipal.GetUserIdFromUser();
            if (userId == null)
            {
                throw new AuthPermissionsException(
                    $"The JTW token didn't contain a claim holding the UserId. Token = {tokenAndRefresh.Token}");
            }
            //b) Create a JWT containing the same data, but with a new Expired time
            var claims = await _claimsCalculator.GetClaimsForAuthUserAsync(userId);
            var tokenAndDesc = GenerateJwtTokenHandler(userId, claims);
            var token = tokenAndDesc.tokenHandler.CreateToken(tokenAndDesc.tokenDescriptor);
            //c) Mark the refreshTokenFromDb as used
            refreshTokenFromDb.MarkAsInvalid();
            //d) Create a new RefreshToken and write to the database
            var newRefreshToken = RefreshToken.CreateNewRefreshToken(userId, token.Id);

            _context.Add(newRefreshToken);
            var status = await _context.SaveChangesWithChecksAsync(new StubDefaultLocalizer());
            status.IfErrorsTurnToException();

            return (new TokenAndRefreshToken
            {
                Token = tokenAndDesc.tokenHandler.WriteToken(token),
                RefreshToken = newRefreshToken.TokenValue
            }, 200);
        }


        //------------------------------------------------------------------
        // private methods

        /// <summary>
        /// Shared code for creating the JWT tokenHandler
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        private (JwtSecurityTokenHandler tokenHandler, SecurityTokenDescriptor tokenDescriptor)
            GenerateJwtTokenHandler(string userId, IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_options.ConfigureAuthPJwtToken.SigningKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                Issuer = _options.ConfigureAuthPJwtToken.Issuer,
                Audience = _options.ConfigureAuthPJwtToken.Audience,
                Expires = DateTime.UtcNow.Add(_options.ConfigureAuthPJwtToken.TokenExpires),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Claims = claims.ToDictionary(x => x.Type, y => (object)y.Value)
            };
            return (tokenHandler, tokenDescriptor);
        }

        /// <summary>
        /// This will extract the ClaimsPrincipal from an expired JWT token
        /// taken from https://www.blinkingcaret.com/2018/05/30/refresh-tokens-in-asp-net-core-web-api/
        /// </summary>
        /// <param name="token">a valid JWT token - can be expired</param>
        /// <returns>If valid JWT (but can be expired) it returns the ClaimsPrincipal. returns null if invalid</returns>
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.ConfigureAuthPJwtToken.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.ConfigureAuthPJwtToken.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_options.ConfigureAuthPJwtToken.SigningKey)),
                ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            //NOTE you check that the Alg is HmacSha256, not HmacSha256Signature (I don't know why)
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
    }
}
