// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Example5.MvcWebApp.AzureAdB2C.AzureAdCode;
using Microsoft.Extensions.DependencyInjection;
using TestSupport.Attributes;
using TestSupport.Helpers;
using Xunit;
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
        public async Task TestOldGraphSyncAzureAdUsersAsync()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var services = new ServiceCollection();
            services.Configure<AzureAdOptions>(config.GetSection("AzureAd"));
            services.AddHttpClient();
            services.AddTransient<ISyncAuthenticationUsers, SyncAzureAdUsers>();
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

    }
}