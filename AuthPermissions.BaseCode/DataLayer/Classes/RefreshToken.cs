// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.BaseCode.DataLayer.Classes
{
    /// <summary>
    /// This holds the information of a RefreshToken sent to the application
    /// This allows for checking that the RefreshToken sent in is correct or not.
    /// </summary>
    public class RefreshToken
    {
        private RefreshToken(string tokenValue, string userId, string jwtId, DateTime addedDateUtc)
        {
            TokenValue = tokenValue;
            UserId = userId;
            JwtId = jwtId;
            AddedDateUtc = addedDateUtc;
        }

        /// <summary>
        /// This is the string value sent to the the caller as a refresh token
        /// It also the primary key to this 
        /// </summary>
        public string TokenValue { get; private set; }

        /// <summary>
        /// The ID of the user linked to this RefreshToken
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        /// <summary>
        /// This takes the Id of the JWT token 
        /// </summary>
        public string JwtId { get; private set; } // Map the token with jwtId

        /// <summary>
        /// If this true, then you should not renew the JWT token
        /// It gets set to true if it has been used, or can manually set to true to force a new login
        /// </summary>
        public bool IsInvalid { get; private set; }

        /// <summary>
        /// This is set to the database utc date when added to the database
        /// </summary>
        public DateTime AddedDateUtc { get; private set; }


        /// <summary>
        /// This is called to create a RefreshToken. It should be written to the database
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="jwtTokenId"></param>
        /// <returns></returns>
        public static RefreshToken CreateNewRefreshToken(string userId, string jwtTokenId)
        {
            //see https://www.blinkingcaret.com/2018/05/30/refresh-tokens-in-asp-net-core-web-api/
            var randomNumber = new byte[AuthDbConstants.RefreshTokenRandomByteSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var tokenValue = Convert.ToBase64String(randomNumber);

            return new RefreshToken(tokenValue, userId, jwtTokenId, DateTime.UtcNow);
        }

        /// <summary>
        /// Use this if a) RefreshToken has been used, or b) you want to stop the user from being able refresh their token
        /// </summary>
        public void MarkAsInvalid()
        {
            IsInvalid = true;
        }
    }

}