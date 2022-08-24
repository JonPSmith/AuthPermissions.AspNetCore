// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.PolicyCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.PermissionsCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Moq;
using Test.StubClasses;
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

        [Fact]
        public void TestHasPermissionFluent()
        {
            //SETUP
            var endpointConventionBuilderMock = new Mock<IEndpointConventionBuilder>();
            var stubEndpointBuilder = new StubEndpointBuilder();

            endpointConventionBuilderMock.Setup(e => e.Add(It.IsAny<Action<EndpointBuilder>>()))
                .Callback<Action<EndpointBuilder>>(x =>
                {
                    x.Invoke(stubEndpointBuilder);
                })
                .Verifiable();

            //ATTEMPT
            endpointConventionBuilderMock.Object.HasPermission(TestEnum.One);
            var authorizeDataObject = stubEndpointBuilder.Metadata.FirstOrDefault();
            var hasPermissionAttribute = authorizeDataObject as HasPermissionAttribute;

            //VERIFY
            endpointConventionBuilderMock.Verify(x => x.Add(It.IsAny<Action<EndpointBuilder>>()), Times.Once);
            hasPermissionAttribute.ShouldNotBeNull();
            hasPermissionAttribute.Policy.ShouldEqual(TestEnum.One.ToString());
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

            var authOptions = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };

            var policyHandler = new PermissionPolicyHandler(authOptions);
            var requirement = new PermissionRequirement( $"{enumToTest}");
            var aspnetContext = new AuthorizationHandlerContext(new List<IAuthorizationRequirement>{ requirement }, user, null);

            //ATTEMPT
            await policyHandler.HandleAsync(aspnetContext);

            //VERIFY
            aspnetContext.HasSucceeded.ShouldEqual(isAllowed);
        }
    }
}