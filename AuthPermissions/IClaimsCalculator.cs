// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthPermissions
{
    public interface IClaimsCalculator
    {
        /// <summary>
        /// This will return the 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<List<Claim>> GetClaimsForAuthUser(string userId);
    }
}