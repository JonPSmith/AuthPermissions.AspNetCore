// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.PermissionsCode;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestPermissionDisplay
    {
        private readonly ITestOutputHelper _output;

        public TestPermissionDisplay(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestGetPermissionsToDisplayNotIncludingFilteredPermission()
        {
            //SETUP

            //ATTEMPT
            var result = PermissionDisplay.GetPermissionsToDisplay(typeof(Example1Permissions), true);

            //VERIFY
            result.Select(x => x.PermissionName).ShouldEqual(new string[]
            {
                Example1Permissions.Permission1.ToString(), Example1Permissions.Permission2.ToString()
            });
            foreach (var permissionDisplay in result)
            {
                _output.WriteLine(permissionDisplay.ToString());
            }
        }

        [Fact]
        public void TestGetPermissionsToDisplayIncludingFilteredPermission()
        {
            //SETUP

            //ATTEMPT
            var result = PermissionDisplay.GetPermissionsToDisplay(typeof(Example1Permissions), false);

            //VERIFY
            result.Select(x => x.PermissionName).ShouldEqual(new string[]
            {
                Example1Permissions.Permission1.ToString(), Example1Permissions.Permission2.ToString(), Example1Permissions.AccessAll.ToString()
            });
            foreach (var permissionDisplay in result)
            {
                _output.WriteLine(permissionDisplay.ToString());
            }
        }

    }
}