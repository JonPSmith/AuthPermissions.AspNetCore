// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.PermissionsCode;
using Test.TestHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestPermissionChecker
    {

        [Theory]
        [InlineData(TestEnum.One, true)]
        [InlineData(TestEnum.Two, false)]
        public void TestThisPermissionIsAllowed(TestEnum enumToTest, bool isAllowed)
        {
            //SETUP
            var packed = $"{(char)1}{(char)3}";

            //ATTEMPT
            var result = typeof(TestEnum).ThisPermissionIsAllowed(packed, enumToTest.ToString());

            //VERIFY
            result.ShouldEqual(isAllowed);
        }

        [Theory]
        [InlineData(TestEnum.One, true)]
        [InlineData(TestEnum.Two, false)]
        public void TestUnpackPermissionsFromString(TestEnum enumToTest, bool isAllowed)
        {
            //SETUP
            var packed = $"{(char)1}{(char)3}";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(PermissionConstants.PackedPermissionClaimType, packed),
            }, "TestAuthentication"));

            //ATTEMPT
            var result = user.UserHasThisPermission(enumToTest);

            //VERIFY
            result.ShouldEqual(isAllowed);
        }
    }
}