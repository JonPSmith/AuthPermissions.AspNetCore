// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.Logging;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestTokenBuilder
    {

        [Fact]
        public async Task TestGenerateJwtTokenAsyncOk()
        {
            //SETUP
            var jwtOptions = SetupHelpers.CreateJwtDataOptions();
            var claimsCalc = new StubClaimsCalculator("This:That");
            var logs = new List<LogOutput>();
            var logger = new Logger<TokenBuilder>(new LoggerFactory(new[] { new MyLoggerProviderActionOut(logs.Add) }));
            var service = new TokenBuilder(jwtOptions, claimsCalc, null, logger);

            //ATTEMPT
            var token = await service.GenerateJwtTokenAsync("User1");

            //VERIFY
            var claims = jwtOptions.Value.TestGetPrincipalFromToken(token).Claims.ToList();
            claims.ClaimsShouldContains(ClaimTypes.NameIdentifier, "User1");
            claims.ClaimsShouldContains("This:That");
        }

        [Fact]
        public async Task TestGenerateTokenAndRefreshTokenAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.ChangeTracker.Clear();

            var jwtOptions = SetupHelpers.CreateJwtDataOptions();
            var authOptions = new AuthPermissionsOptions { TenantType = TenantTypes.NotUsingTenants };
            var claimsCalc = new ClaimsCalculator(context, authOptions);
            var logs = new List<LogOutput>();
            var logger = new Logger<TokenBuilder>(new LoggerFactory(new[] { new MyLoggerProviderActionOut(logs.Add) }));
            var service = new TokenBuilder(jwtOptions, claimsCalc, context, logger);

            //ATTEMPT
            var token = await service.GenerateJwtTokenAsync("User1");

            //VERIFY

        }
    }
}