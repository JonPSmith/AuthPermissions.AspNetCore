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
            _serviceProvider = this.SetupServicesForTest(true);
        }

        [Fact]
        public void TestDemoSetup()
        {
            //SETUP

            //ATTEMPT
            var demoSetup = (IOptions<DemoSetup>)_serviceProvider.GetService(typeof(IOptions<DemoSetup>));

            //VERIFY
            demoSetup.Value.ShouldNotBeNull();
            demoSetup.Value.Users.Length.ShouldEqual(4);
        }

        [Fact]
        public async Task TestStartAsync()
        {
            //SETUP
            var setupUsersService = new SetupAspNetCoreUsers(_serviceProvider);

            //ATTEMPT
            await setupUsersService.StartAsync(default);

            //VERIFY
            using var userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var users = userManager.Users.ToList();
            users.Count.ShouldEqual(4);
            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                (roles.Any() == !user.Email.StartsWith("No")).ShouldBeTrue();
                _output.WriteLine($"{user.Email}: Roles = {string.Join(',', roles)}");
            }
        }
    }
}