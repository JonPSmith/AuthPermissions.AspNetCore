// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using ExamplesCommonCode.DemoSetupCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Test.DiTestHelpers;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAspNetCore
{
    public class TestSetupAspNetSetupDatabaseOnStartup
    {
        private readonly ITestOutputHelper _output;

        public TestSetupAspNetSetupDatabaseOnStartup(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestSetupDatabaseOnStartupOnlySuperUser()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupDatabaseOnStartup();

            var serviceProvider = services.BuildServiceProvider();
            var startupService = serviceProvider.GetRequiredService<IHostedService>();

            //ATTEMPT
            await startupService.StartAsync(default);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var users = userManager.Users.ToList();
            users.Count.ShouldEqual(1);
        }
    }
}