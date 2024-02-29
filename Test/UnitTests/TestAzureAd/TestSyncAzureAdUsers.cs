// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AzureAdServices;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using TestSupport.Attributes;
using TestSupport.Helpers;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAzureAd
{
    public class TestSyncAzureAdUsers
    {
        private readonly ITestOutputHelper _output;

        public TestSyncAzureAdUsers(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// This test takes some time so only run it manually in Debug
        /// </summary>
        /// <returns></returns>
        [RunnableInDebugOnly]
        public async Task TestSyncAzureAdUsersService()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var services = new ServiceCollection();
            services.Configure<AzureAdOptions>(config.GetSection("AzureAd"));
            services.AddTransient<ISyncAuthenticationUsers, AzureAdAccessService>();
            services.AddSingleton("en".SetupAuthPLoggingLocalizer());
            var serviceProvider = services.BuildServiceProvider();
            
            var service = serviceProvider.GetService<ISyncAuthenticationUsers>();
            service.ShouldNotBeNull();

            //ATTEMPT
            var users = await service.GetAllActiveUserInfoAsync();

            //VERIFY
            foreach (var user in users)
            {
                _output.WriteLine($"Email: {user.Email}, Name: {user.UserName}, UserId: {user.UserId}");
            }
            users.Any().ShouldBeTrue();
        }

        /// <summary>
        /// This test takes some time so only run it manually in Debug
        /// </summary>
        /// <returns></returns>
        [RunnableInDebugOnly]
        public async Task TestAzureAdAccessService_FindAzureUserAsync()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var services = new ServiceCollection();
            services.Configure<AzureAdOptions>(config.GetSection("AzureAd"));
            services.AddTransient<IAzureAdAccessService, AzureAdAccessService>();
            services.AddSingleton("en".SetupAuthPLoggingLocalizer());
            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IAzureAdAccessService>();
            service.ShouldNotBeNull();

            //ATTEMPT
            var userId = await service.FindAzureUserAsync("bad@authpermissions.onmicrosoft.com");

            //VERIFY
            _output.WriteLine(userId ?? "< null >");
        }
    }
}