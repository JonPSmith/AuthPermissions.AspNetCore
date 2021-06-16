// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.JwtTokenCode
{
    public interface ITokenBuilder
    {
        Task<string> GenerateJwtTokenAsync(string userId);

        public Task<TokenAndRefreshToken> GenerateTokenAndRefreshTokenAsync(string userId);
    }
}