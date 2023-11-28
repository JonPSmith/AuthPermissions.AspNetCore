// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.PermissionsCode.Services;
using Test.TestHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestUsersPermissions
    {
        [Theory]
        [InlineData(TestEnum.Two, "One,Two")]
        [InlineData(TestEnum.Three, "One,Three")]
        public void TestPermissionsFromClaims(TestEnum enumToTest, string commaDelimited)
        {
            //SETUP
            var packed = $"{(char)1}{(char)(enumToTest)}";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(PermissionConstants.PackedPermissionClaimType, packed),
            }, "TestAuthentication"));

            var options = new AuthPermissionsOptions {InternalData = {EnumPermissionsType = typeof(TestEnum)}};
            var service = new UsersPermissionsService(options);

            //ATTEMPT
            var names = service.PermissionsFromUser(user);

            //VERIFY
            string.Join(",",names).ShouldEqual(commaDelimited);
        }
    }
}