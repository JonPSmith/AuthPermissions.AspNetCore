// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.PermissionsCode;
using Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;
using Xunit;
using Xunit.Extensions.AssertExtensions;
using Example2.WebApiWithToken.IndividualAccounts.PermissionsCode;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;
using Test.StubClasses;

namespace Test.UnitTests.TestExamples;

public class TestExample2UpdateRoleClaimMiddleware
{
    private static ClaimsPrincipal CreateUser(string packedPermissions, string userId = "userId")
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            //NOTE: This uses the JWT Token claim name
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(PermissionConstants.PackedPermissionClaimType, packedPermissions),
        }, "Test"));
        return user;
    }

    private static IServiceProvider GetServiceProvider(StubFileStoreCacheClass stubFsCache)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedFileStoreCacheClass>(stubFsCache);
        return services.BuildServiceProvider();
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestReplacePermissionsMiddleware(bool addPermissionUpdate)
    {
        //SETUP
        var user = CreateUser($"{(char)Example2Permissions.Permission1}");
        var stubFsCache = new StubFileStoreCacheClass();
        if (addPermissionUpdate)
            stubFsCache.Set("userId".FormReplacementPermissionsKey(), $"{(char)Example2Permissions.Permission2}");

        //ATTEMPT
        var newUser = await UpdateRoleClaimMiddleware.ReplacePermissionsMiddleware(GetServiceProvider(stubFsCache), user);

        //VERIFY
        if (addPermissionUpdate)
        {
            newUser.ShouldNotBeNull();
            newUser.GetPackedPermissionsFromUser().ShouldEqual($"{(char)Example2Permissions.Permission2}");
        }
        else
        {
            newUser.ShouldBeNull();
            user.GetPackedPermissionsFromUser().ShouldEqual($"{(char)Example2Permissions.Permission1}");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestReplacePermissionsMiddleware_NoRoles(bool addPermissionUpdate)
    {
        //SETUP
        var user = CreateUser($"{(char)Example2Permissions.Permission1}");
        var stubFsCache = new StubFileStoreCacheClass();
        if (addPermissionUpdate)
            stubFsCache.Set("userId".FormReplacementPermissionsKey(), "");

        //ATTEMPT
        var newUser = await UpdateRoleClaimMiddleware.ReplacePermissionsMiddleware(GetServiceProvider(stubFsCache), user);

        //VERIFY
        if (addPermissionUpdate)
        {
            newUser.ShouldNotBeNull();
            newUser.GetPackedPermissionsFromUser().ShouldEqual("");
        }
        else
        {
            newUser.ShouldBeNull();
            user.GetPackedPermissionsFromUser().ShouldEqual($"{(char)Example2Permissions.Permission1}");
        }
    }

}