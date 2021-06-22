// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
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
        public void TestPackPermissionsIntoString()
        {
            //SETUP

            //ATTEMPT
            var result = PermissionDisplay.GetPermissionsToDisplay(typeof(Example1Permissions));

            //VERIFY
            result.Count.ShouldEqual(3);
            foreach (var permissionDisplay in result)
            {
                _output.WriteLine(permissionDisplay.ToString());
            }
        }
    }
}