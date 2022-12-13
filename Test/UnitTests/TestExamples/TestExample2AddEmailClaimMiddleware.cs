// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;
using Example2.WebApiWithToken.IndividualAccounts.PermissionsCode;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using Xunit;
using Xunit.Extensions.AssertExtensions;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using TestSupport.EfHelpers;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using LocalizeMessagesAndErrors;
using Test.StubClasses;
using Test.TestHelpers;

namespace Test.UnitTests.TestExamples;

public class TestExample2AddEmailClaimMiddleware
{
    private static ClaimsPrincipal CreateUser(string userId = "userId")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            //NOTE: This uses the JWT Token claim name
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "Test"));
        return user;
    }

    private static IServiceProvider GetServiceProvider(StubFileStoreCacheClass stubFsCache)
    {
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options, new []{new EmailChangeDetectorService(stubFsCache) });
        context.Database.EnsureCreated();
        context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("userId", "email@google.com", null));
        context.SaveChanges();

        var services = new ServiceCollection();
        services.AddSingleton<IDistributedFileStoreCacheClass>(stubFsCache);
        services.AddSingleton(context);
        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestAddEmailClaimToCurrentUser_AddIfMissing(bool emailAlreadyInCache)
    {
        //SETUP
        var user = CreateUser();
        var stubFsCache = new StubFileStoreCacheClass();
        if (emailAlreadyInCache)
            stubFsCache.Set("userId".FormAddedEmailClaimKey(), "cached email");

        //ATTEMPT
        var newUser = await AddEmailClaimMiddleware.AddEmailClaimToCurrentUser(GetServiceProvider(stubFsCache), user);

        //VERIFY
        newUser.ShouldNotBeNull();
        var emailInClaim = newUser.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        emailInClaim.ShouldEqual(emailAlreadyInCache ? "cached email" : "email@google.com");
    }

    [Fact]
    public async Task TestAddEmailClaimToCurrentUser_NoUserFound()
    {
        //SETUP
        var user = CreateUser("not found userId");
        var stubFsCache = new StubFileStoreCacheClass();

        //ATTEMPT
        var newUser = await AddEmailClaimMiddleware.AddEmailClaimToCurrentUser(GetServiceProvider(stubFsCache), user);

        //VERIFY
        newUser.ShouldBeNull();
    }

    [Fact]
    public void TestEmailChangeDetectorService_EmailChanged()
    {
        //SETUP
        var stubFsCache = new StubFileStoreCacheClass();
        var serviceProvider = GetServiceProvider(stubFsCache);

        var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

        context.ChangeTracker.Clear();
        stubFsCache.Set("userId".FormAddedEmailClaimKey(), "cached email");

        //ATTEMPT
        var user = context.AuthUsers.Single();
        user.ChangeUserNameAndEmailWithChecks("second@gmail.com", null);
        context.SaveChanges();

        //VERIFY
        stubFsCache.Get("userId".FormAddedEmailClaimKey()).ShouldBeNull();
    }

}