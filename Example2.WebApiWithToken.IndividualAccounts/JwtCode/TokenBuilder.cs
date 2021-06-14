// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Example2.WebApiWithToken.IndividualAccounts.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Example2.WebApiWithToken.IndividualAccounts.JwtCode
{
    public class TokenBuilder : ITokenBuilder
    {
        private readonly JwtData _jwtData;

        public TokenBuilder(IOptions<JwtData> jwtData)
        {
            _jwtData = jwtData.Value;
        }

        public string GenerateJwtToken(IdentityUser user, IEnumerable<Claim> claims)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtData.SigningKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }),
                Issuer = _jwtData.Issuer,
                Audience = _jwtData.Audience,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Claims = claims.ToDictionary(x => x.Type, y => (object)y.Value)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}