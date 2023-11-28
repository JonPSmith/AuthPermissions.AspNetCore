// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.PermissionsCode;
using Test.TestHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestPermissionPacker
    {
        [Fact]
        public void TestPackPermissionsIntoString()
        {
            //SETUP
            var enums = new TestEnum[] {TestEnum.One, TestEnum.Three};

            //ATTEMPT
            var packed = typeof(TestEnum).PackPermissionsNames(enums.Select(x => x.ToString())) ;

            //VERIFY
            packed.ShouldEqual($"{(char)1}{(char)3}");
        }

        [Fact]
        public void TestUnpackPermissionsFromString()
        {
            //SETUP

            //ATTEMPT
            var packed = typeof(TestEnum).PackCommaDelimitedPermissionsNames("One, Three") ;

            //VERIFY
            packed.ShouldEqual($"{(char)1}{(char)3}");
        }
    }
}