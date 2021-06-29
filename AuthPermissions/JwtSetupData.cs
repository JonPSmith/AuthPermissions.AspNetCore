// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace AuthPermissions.AspNetCore.JwtTokenCode
{
    /// <summary>
    /// This contains the data that the JWT token (and optional RefreshToken)
    /// </summary>
    public class JwtSetupData
    {
        /// <summary>
        /// This identifies provider that issued the JWT
        /// </summary>
        public string Issuer { get; set; }
        /// <summary>
        /// This identifies the recipients that the JWT is intended for
        /// </summary>
        public string Audience { get; set; }
        /// <summary>
        /// This is a SECRET key that both the issuer and audience have to have 
        /// </summary>
        public string SigningKey { get; set; }
        /// <summary>
        /// JWT Token' `Expires` property is set to a date by added a `TokenExpires` timespan to the current Datetime.UtcNow
        /// </summary>
        public TimeSpan TokenExpires { get; set; }
        /// <summary>
        /// This Timespan is used to work out if the RefreshToken (in the database) has expired or not
        /// </summary>
        public TimeSpan RefreshTokenExpires { get; set; }
    }
}