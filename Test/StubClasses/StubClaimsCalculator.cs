// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions;

namespace Test.StubClasses
{
    public class StubClaimsCalculator : IClaimsCalculator
    {
        public StubClaimsCalculator(string nameValuePairs = "Name:user,Data1:mydata")
        {
            foreach (var nameValueString in nameValuePairs.Split(','))
            {
                var nameValue = nameValueString.Split(':');
                Claims.Add(new Claim(nameValue[0], nameValue[1]));
            }
        }

        public List<Claim> Claims { get; set; } = new List<Claim>();

        public Task<List<Claim>> GetClaimsForAuthUserAsync(string userId)
        {
            return Task.FromResult(Claims);
        }
    }
}