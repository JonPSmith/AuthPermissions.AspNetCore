// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using ExamplesCommonCode.DemoSetupCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Test.DiTestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExampleCommonCode
{
    public class TestSetupAspNetCoreUsers
    {
        private readonly ITestOutputHelper _output;
        private readonly ServiceProvider _serviceProvider;

        public TestSetupAspNetCoreUsers(ITestOutputHelper output)
        {
            _output = output;
            _serviceProvider = this.SetupServicesForTest().BuildServiceProvider();
        }

        [Fact]
        public async Task TestAddDemoUsersAsync()
        {
            //SETUP
            var userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            //ATTEMPT
            await userManager.CheckAddNewUserAsync("user1@G.com", "Pass!W0rd");
            await userManager.CheckAddNewUserAsync("user2@G.com", "Pass!W0rd");

            //VERIFY
            var users = userManager.Users.ToList();
            users.Count.ShouldEqual(2);
            foreach (var user in users)
            {
                _output.WriteLine($"{user.Email}");
            }
        }
    }
}