// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreCode;
using Example3.MvcWebApp.IndividualAccounts.Data;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestIndividualUserAddUserManager
{
    private readonly ITestOutputHelper _output;

    private readonly ServiceProvider _serviceProvider;

    public TestIndividualUserAddUserManager(ITestOutputHelper output)
    {
        //var identityOptions = SqliteInMemory.CreateOptions<ApplicationDbContext>();
        //using var identityContext = new ApplicationDbContext(identityOptions);
        //identityContext.Database.EnsureCreated();
        //var authOptions = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        //using var authContext = new AuthPermissionsDbContext(authOptions);
        //authContext.Database.EnsureCreated();

        //var services = new ServiceCollection();
        //services.AddIdentity<IdentityUser, IdentityRole>()
        //    .AddEntityFrameworkStores<ApplicationDbContext>();

        //var serviceProvider = services.BuildServiceProvider();
        //var authSettings = new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel };

        //var individualUserManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        //var individualUserSign = serviceProvider.GetRequiredService<SignInManager<IdentityUser>>();
        //var authUserAdmin = new AuthUsersAdminService(authContext, null, authSettings);
        //var authTenantAdmin =
        //    new AuthTenantAdminService(authContext, authSettings, new StubITenantChangeServiceFactory(), null);

        //_userManager = new IndividualUserAddUserManager<IdentityUser>(authUserAdmin, authTenantAdmin,
        //    individualUserManager, individualUserSign);


        _output = output;
        this.GetUniqueDatabaseConnectionString();

        var services = new ServiceCollection();
        //Wanted to use the line below but just couldn't get the right package for it
        //services.AddDefaultIdentity<IdentityUser>()
        services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        var startupConfig = AppSettings.GetConfiguration();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(startupConfig);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(this.GetUniqueDatabaseConnectionString("Individual")));
        services.AddDbContext<InvoicesDbContext>(options =>
            options.UseSqlServer(this.GetUniqueDatabaseConnectionString("Invoice"), dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.InvoicesDbContextHistoryName)));

        services.AddScoped<IGetDataKeyFromUser>(x => new StubGetDataKeyFilter(""));
        services.RegisterAuthPermissions<Example3Permissions>(options =>
        {
            options.TenantType = TenantTypes.SingleLevel;
            options.EncryptionKey = "asffrwedsffsgxcvwc";
            options.UseLocksToUpdateGlobalResources = false;
        })
            .UsingEfCoreSqlServer(this.GetUniqueDatabaseConnectionString("AuthP"))
            .IndividualAccountsAuthentication()
            .RegisterTenantChangeService<InvoiceTenantChangeService>()
            .SetupAspNetCorePart();

        //Add the SupportCode services
        services.AddTransient<IAuthenticationAddUserManager, IndividualUserAddUserManager<IdentityUser>>();
        services.AddTransient<ISignInAndCreateTenant, SignInAndCreateTenant>();
        services.AddTransient<IInviteNewUserService, InviteNewUserService>();

        _serviceProvider = services.BuildServiceProvider();


        var accountContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        accountContext.Database.EnsureCreated();
        var authContext = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
        authContext.Database.EnsureClean();
        var invoiceContext = _serviceProvider.GetRequiredService<InvoicesDbContext>();
        invoiceContext.Database.EnsureClean();
    }

    [Theory]
    [InlineData("User2@gmail.com", false)]
    [InlineData("AnotherEmail", true)]
    public async Task TestCheckNoExistingAuthUserAsync_Email(string email, bool isValid)
    {
        //SETUP
        var context = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
        context.Database.EnsureClean();
        context.AddMultipleUsersWithRolesInDb();

        var service = _serviceProvider.GetRequiredService<IAuthenticationAddUserManager>();
        var userData = new AddNewUserDto { Email = email };

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await service.CheckNoExistingAuthUserAsync(userData);

        //VERIFY
        status.IsValid.ShouldEqual(isValid);
    }

    [Fact]
    public async Task TestSetUserInfoAsyncOk()
    {
        //SETUP
        var context = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
        context.Database.EnsureClean();
        await context.SetupRolesInDbAsync();

        var service = _serviceProvider.GetRequiredService<IAuthenticationAddUserManager>();
        var userData = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "Role1", "Role2" } };

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await service.SetUserInfoAsync(userData, "Pass!3ord");

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var userAdmin = _serviceProvider.GetRequiredService<IAuthUsersAdminService>();
        var user = (await userAdmin.FindAuthUserByEmailAsync(userData.Email)).Result;
        user.UserRoles.Select(x => x.RoleName).ShouldEqual(new [] { "Role1", "Role2" });
    }

    //Can't test IndividualUserAddUserManager.LoginVerificationAsync because it needs a HttpContext

    //[Fact]
    //public async Task TestLoginVerificationAsync()
    //{
    //    //SETUP
    //    var context = _serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
    //    context.Database.EnsureClean();
    //    await context.SetupRolesInDbAsync();

    //    var service = _serviceProvider.GetRequiredService<IAuthenticationAddUserManager>();
    //    var newUser = new AddNewUserDto { Email = "me@gmail.com", Roles = new() { "Role1", "Role2" } };
    //    (await service.SetUserInfoAsync(newUser, "Pass!3ord")).IsValid.ShouldBeTrue();

    //    context.ChangeTracker.Clear();

    //    //ATTEMPT
    //    var status = await service.LoginVerificationAsync(newUser.Email, newUser.UserName);

    //    //VERIFY
    //    context.ChangeTracker.Clear();
    //    status.IsValid.ShouldBeTrue(status.GetAllErrors());
    //}
}