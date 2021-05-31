// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.PolicyCode;
using AuthPermissions.PermissionsCode;
using Microsoft.AspNetCore.Authorization;
using Test.TestHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAspNetCore
{
    public class TestPermissionPolicy
    {

        [HasPermission(TestEnum.Two)]
        private class WithAutoPermissions
        {}

        [Fact]
        public void TestHasPermissionAttribute()
        {
            //SETUP

            //ATTEMPT
            var authAtt = typeof(WithAutoPermissions).GetCustomAttribute<AuthorizeAttribute>();

            //VERIFY
            authAtt.ShouldNotBeNull();
            authAtt.Policy.ShouldEqual(TestEnum.Two.ToString());
        }

        [Theory]
        [InlineData(TestEnum.One, true)]
        [InlineData(TestEnum.Two, false)]
        public async Task TestPermissionPolicyHandler(TestEnum enumToTest, bool isAllowed)
        {
            //SETUP
            var packed = $"{(char)1}{(char)3}";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(PermissionConstants.PackedPermissionClaimType, packed),
            }, "TestAuthentication"));

            var policyHandler = new PermissionPolicyHandler(new EnumTypeService(typeof(TestEnum)));
            var requirement = new PermissionRequirement( $"{enumToTest}");
            var aspnetContext = new AuthorizationHandlerContext(new List<IAuthorizationRequirement>{ requirement }, user, null);

            //ATTEMPT
            await policyHandler.HandleAsync(aspnetContext);

            //VERIFY
            aspnetContext.HasSucceeded.ShouldEqual(isAllowed);
        }
    }
}