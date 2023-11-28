// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.PermissionsCode;
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
            var result = user.HasPermission(enumToTest);

            //VERIFY
            result.ShouldEqual(isAllowed);
        }

        [Fact]
        public void TestThrowExceptionIfEnumIsNotCorrect()
        {
            //SETUP
            

            //ATTEMPT
            Assert.Throws<AuthPermissionsException>( () => typeof(BadEnum).ThrowExceptionIfEnumIsNotCorrect());

            //VERIFY
        }

        [Fact]
        public void TestThrowExceptionIfEnumHasMembersHaveDuplicateValues()
        {
            //SETUP


            //ATTEMPT
            var ex = Assert.Throws<AuthPermissionsException>(() => typeof(DuplicateEnum).ThrowExceptionIfEnumHasMembersHaveDuplicateValues());

            //VERIFY
            ex.Message.Split('\n').ShouldEqual(new string[]
            {
                "The following enum members in the 'DuplicateEnum' enum have the same value, which will cause problems",
                "One, Two all have the value 1",
                "Four, Five, Six all have the value 4"
            });
        }


        enum BadEnum {One, Two};

        enum  DuplicateEnum : ushort { One = 1, Two = 1, Three = 3, Four = 4, Five = 4, Six = 4 };
    }
}