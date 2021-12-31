// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Example3.InvoiceCode.Dtos;
using Example3.InvoiceCode.EfCoreCode;
using Example3.InvoiceCode.Services;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
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
            services.AddTransient<IUserRegisterInviteService, UserRegisterInviteService>();
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
            var accountContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
            accountContext.Database.EnsureClean();
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.Database.Migrate();
        }

        private static async Task SetupExample3Roles(AuthPermissionsDbContext authContext)
        {
            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(Example3Permissions);
            var roleLoader = new BulkLoadRolesService(authContext, authOptions);
            await roleLoader.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);
        }

        [Fact]
        public async Task TestCreateNewTenantAsyncOk()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            authContext.ChangeTracker.Clear();

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = "Free"
            };

            //ATTEMPT
            var status = await service.AddUserAndNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            authContext.Tenants.Single().TenantFullName.ShouldEqual("TestTenant");
            var newUser = authContext.AuthUsers.Include(x => x.UserTenant).Single();
            newUser.Email.ShouldEqual(createTenantDto.Email);
            newUser.UserTenant.TenantFullName.ShouldEqual("TestTenant");
        }

        [Theory]
        [InlineData("Free", null, "Tenant User")]
        [InlineData("Pro", "Tenant Admin", "Tenant Admin,Tenant User")]
        [InlineData("Enterprise", "Tenant Admin", "Enterprise,Tenant Admin,Tenant User")]
        public async Task TestCreateNewTenantAsyncCheckRolesAdded(string version, string expectedUserRole, string expectedTenantRoles)
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            authContext.ChangeTracker.Clear();

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = version
            };

            //ATTEMPT
            var status = await service.AddUserAndNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            authContext.Tenants.Include(x => x.TenantRoles).Single()
                .TenantRoles.Select(x => x.RoleName).ShouldEqual(expectedTenantRoles.Split(',').ToList());
            authContext.AuthUsers.Include(x => x.UserRoles).Single()
                .UserRoles.Select(x => x.RoleName).ShouldEqual(expectedUserRole?.Split(',').ToList() ?? new ());
        }

        [Fact]
        public async Task TestCreateNewTenantAsyncTenantAlreadyThere()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            authContext.ChangeTracker.Clear();

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = "Free"
            };
            var setupStatus = await service.AddUserAndNewTenantAsync(createTenantDto);
            setupStatus.IsValid.ShouldBeTrue(setupStatus.GetAllErrors());

            authContext.ChangeTracker.Clear();

            //ATTEMPT
            var status = await service.AddUserAndNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("The tenant name 'TestTenant' is already taken");
        }

        [Fact]
        public async Task TestCreateNewTenantAsyncUserAlreadyThere()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();
            var createTenantDto = new CreateTenantDto
            {
                TenantName = "TestTenant",
                Email = "User1@gmail.com",
                Password = "User1@gmail.com",
                Version = "Free"
            };
            var setupStatus = await service.AddUserAndNewTenantAsync(createTenantDto);
            setupStatus.IsValid.ShouldBeTrue(setupStatus.GetAllErrors());

            authContext.ChangeTracker.Clear();

            //ATTEMPT
            createTenantDto.TenantName = "DifferentName";
            var status = await service.AddUserAndNewTenantAsync(createTenantDto);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("You are already registered as a user, which means you can't ask to access another tenant.");
        }

        //--------------------------------------------------------
        //AcceptUserJoiningATenantAsync

        [Fact]
        public async Task TestAcceptUserJoiningATenantOk()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            var newTenant = Tenant.CreateSingleTenant("Test Tenant").Result;
            authContext.Add(newTenant);
            authContext.SaveChanges();

            authContext.ChangeTracker.Clear();

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();

            var verify = service.InviteUserToJoinTenantAsync(newTenant.TenantId, "User1@gmail.com");

            //ATTEMPT
            var status = await service.AcceptUserJoiningATenantAsync("User1@gmail.com", "User1@gmail.com", verify);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            authContext.ChangeTracker.Clear();
            var addedUser = authContext.AuthUsers
                .Include(x => x.UserTenant).Single();
            addedUser.Email.ShouldEqual("User1@gmail.com");
            addedUser.UserTenant.TenantFullName.ShouldEqual("Test Tenant");
        }

        [Fact]
        public async Task TestAcceptUserJoiningATenantEmailMismatch()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            var newTenant = Tenant.CreateSingleTenant("Test Tenant").Result;
            authContext.Add(newTenant);
            authContext.SaveChanges();

            authContext.ChangeTracker.Clear();

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();

            var verify = service.InviteUserToJoinTenantAsync(newTenant.TenantId, "User1@gmail.com");

            //ATTEMPT
            var status = await service.AcceptUserJoiningATenantAsync("Different@gmail.com", "User1@gmail.com", verify);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("Sorry, your email didn't match the invite.");
        }

        [Fact]
        public async Task TestAcceptUserJoiningATenantBadVerify()
        {
            //SETUP
            var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            await SetupExample3Roles(authContext);

            var newTenant = Tenant.CreateSingleTenant("Test Tenant").Result;
            authContext.Add(newTenant);
            authContext.SaveChanges();

            authContext.ChangeTracker.Clear();

            var service = _serviceProvider.GetRequiredService<IUserRegisterInviteService>();

            var verify = "sads3dsfgfg=";

            //ATTEMPT
            var status = await service.AcceptUserJoiningATenantAsync("User1@gmail.com", "User1@gmail.com", verify);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("Sorry, the verification failed.");
        }
    }
}