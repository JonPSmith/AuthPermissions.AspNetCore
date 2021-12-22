// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.Dtos;
using Example3.InvoiceCode.EfCoreCode;
using Example3.InvoiceCode.Services;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.DiTestHelpers;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample3TenantSetupServices
    {
        private readonly ITestOutputHelper _output;
        private readonly ServiceProvider _serviceProvider;

        public TestExample3TenantSetupServices(ITestOutputHelper output)
        {
            _output = output;

            var options = this.CreateUniqueClassOptions<InvoicesDbContext>();
            using var context = new InvoicesDbContext(options, new StubGetDataKeyFilter(""));
            context.Database.EnsureClean();

            var services = this.SetupServicesForTest(true);
            services.AddTransient<ITenantSetupServices, TenantSetupServices>();
            services.AddTransient<IGetDataKeyFromUser>(x => new StubGetDataKeyFilter(""));
            services.RegisterAuthPermissions<Example3Permissions>(options =>
                {
                    options.TenantType = TenantTypes.SingleLevel;
                    options.AppConnectionString = context.Database.GetConnectionString();
                    options.UseLocksToUpdateGlobalResources = false;
                })
                .UsingEfCoreSqlServer(context.Database.GetConnectionString())
                .IndividualAccountsAuthentication()
                .RegisterTenantChangeService<InvoiceTenantChangeService>()
                .SetupAspNetCorePart();
            _serviceProvider = services.BuildServiceProvider();
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.Database.Migrate();
            var accountContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            accountContext.Database.EnsureClean();
        }

        [Fact]
        public async Task TestCreateNewTenantAsyncOk()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            var service = _serviceProvider.GetRequiredService<ITenantSetupServices>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = "Free"
            };

            //ATTEMPT
            var status = await service.CreateNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            authContext.Tenants.Single().TenantFullName.ShouldEqual("TestTenant");
            var newUser = authContext.AuthUsers.Include(x => x.UserTenant).Single();
            newUser.Email.ShouldEqual(createTenantDto.Email);
            newUser.UserTenant.TenantFullName.ShouldEqual("TestTenant");
        }

        [Fact]
        public async Task TestCreateNewTenantAsyncTenantAlreadyThere()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            var service = _serviceProvider.GetRequiredService<ITenantSetupServices>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = "Free"
            };
            var setupStatus = await service.CreateNewTenantAsync(createTenantDto);
            setupStatus.IsValid.ShouldBeTrue(setupStatus.GetAllErrors());

            authContext.ChangeTracker.Clear();

            //ATTEMPT
            var status = await service.CreateNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("The tenant name 'TestTenant' is already taken");
        }

        [Fact]
        public async Task TestCreateNewTenantAsyncUserAlreadyThere()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            var service = _serviceProvider.GetRequiredService<ITenantSetupServices>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = "Free"
            };
            var setupStatus = await service.CreateNewTenantAsync(createTenantDto);
            setupStatus.IsValid.ShouldBeTrue(setupStatus.GetAllErrors());

            authContext.ChangeTracker.Clear();

            //ATTEMPT
            createTenantDto.TenantName = "DifferentName";
            var status = await service.CreateNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("You are already registered as a user.");
        }
    }
}