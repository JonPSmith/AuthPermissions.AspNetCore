// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode;
using Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples;

public class TestExample2EmailChangeDetectorService
{
    [Fact]
    public async Task TestEmailChangeDetectorService_ChangeEmail()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var stubFsCache = new StubFileStoreCacheClass();
        var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> 
            { new EmailChangeDetectorService(stubFsCache) });
        context.Database.EnsureCreated();

        await context.SetupRolesInDbAsync();
        context.AddMultipleUsersWithRolesInDb();

        context.ChangeTracker.Clear();
        stubFsCache.ClearAll();

        var authAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(), 
            new AuthPermissionsOptions(), "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        stubFsCache.Set("User1".FormAddedEmailClaimKey(), "cached email");
        await authAdmin.UpdateUserAsync("User1", email: "new@google.com");

        //VERIFY
        stubFsCache.Get("User1".FormAddedEmailClaimKey()).ShouldBeNull();
    }
}