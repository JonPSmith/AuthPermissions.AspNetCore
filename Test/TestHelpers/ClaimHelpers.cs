// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit.Extensions.AssertExtensions;

namespace Test.TestHelpers
{
    public static class ClaimHelpers
    {
        public static void ClaimsShouldContains(this IEnumerable<Claim> claims, string typeValueString)
        {
            var typeValue = typeValueString.Split(':');
            claims.ClaimsShouldContains(typeValue[0], typeValue[1]);
        }

        public static void ClaimsShouldContains(this IEnumerable<Claim> claims, string type, string value)
        {
            var claim = claims.SingleOrDefault(x => x.Type == type);
            claim.ShouldNotBeNull();
            claim.Value.ShouldEqual(value);
        }

        public static void ClaimShouldEqual(this Claim claim, string typeValueString)
        {
            var typeValue = typeValueString.Split(':');
            claim.ClaimShouldEqual(typeValue[0], typeValue[1]);
        }

        public static void ClaimShouldEqual(this Claim claim, string type, string value)
        {
            claim.Type.ShouldEqual(type);
            claim.Value.ShouldEqual(value);
        }
    }
}