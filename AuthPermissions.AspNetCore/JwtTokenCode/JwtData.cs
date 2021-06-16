// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace AuthPermissions.AspNetCore.JwtTokenCode
{
    /// <summary>
    /// This contains the data that the JWT token (and optional RefreshToken)
    /// </summary>
    public class JwtData
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SigningKey { get; set; }
        public TimeSpan Expires { get; set; }
        public TimeSpan RefreshTokenExpires { get; set; }
    }
}