// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.Extensions.Logging;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestTokenBuilder
    {
        private class SetupTokenBuilder
        {
            private readonly AuthPermissionsDbContext _context;

            public SetupTokenBuilder(AuthPermissionsDbContext context, TimeSpan expiresIn = default)
            {
                _context = context;

                var jwtOptions = SetupHelpers.CreateJwtDataOptions(expiresIn);
                JwtData = jwtOptions.Value;
                var claimsCalc = new StubClaimsCalculator("This:That");
                var logger = new Logger<TokenBuilder>(new LoggerFactory(new[] { new MyLoggerProviderActionOut(Logs.Add) }));
                TokenBuilder = new TokenBuilder(jwtOptions, claimsCalc, context, logger);
            }

            public ITokenBuilder TokenBuilder { get; }
            public JwtData JwtData { get; }
            public List<LogOutput> Logs { get; } = new List<LogOutput>();

        }

        [Fact]
        public async Task TestGenerateJwtTokenAsyncOk()
        {
            //SETUP
            var setup = new SetupTokenBuilder(null);

            //ATTEMPT
            var token = await setup.TokenBuilder.GenerateJwtTokenAsync("User1");

            //VERIFY
            var claims = setup.JwtData.TestGetPrincipalFromToken(token).Claims.ToList();
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

            var setup = new SetupTokenBuilder(context);

            context.ChangeTracker.Clear();

            //ATTEMPT
            var tokenAndRefresh = await setup.TokenBuilder.GenerateTokenAndRefreshTokenAsync("User1");

            //VERIFY
            context.ChangeTracker.Clear();
            var claims = setup.JwtData.TestGetPrincipalFromToken(tokenAndRefresh.Token).Claims.ToList();
            claims.ClaimsShouldContains(ClaimTypes.NameIdentifier, "User1");

            context.RefreshTokens.Count().ShouldEqual(1);
            context.RefreshTokens.Single().TokenValue.ShouldEqual(tokenAndRefresh.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenUsingRefreshTokenAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setup = new SetupTokenBuilder(context);
            var tokenAndRefresh = await setup.TokenBuilder.GenerateTokenAndRefreshTokenAsync("User1");

            context.ChangeTracker.Clear();

            //ATTEMPT
            var tokensAndStatus = await setup.TokenBuilder.RefreshTokenUsingRefreshTokenAsync(tokenAndRefresh);

            //VERIFY
            context.ChangeTracker.Clear();
            tokensAndStatus.HttpStatusCode.ShouldEqual(200);

            var allRefreshTokens = context.RefreshTokens.ToList();
            allRefreshTokens.Count.ShouldEqual(2);
            allRefreshTokens.Single(x => x.TokenValue == tokensAndStatus.updatedTokens.RefreshToken).IsInvalid.ShouldBeFalse();
            allRefreshTokens.Single(x => x.TokenValue != tokensAndStatus.updatedTokens.RefreshToken).IsInvalid.ShouldBeTrue();
        }

        [Fact]
        public async Task RefreshTokenUsingRefreshTokenAsyncRefreshAlreadyUsed()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setup = new SetupTokenBuilder(context);
            var tokenAndRefresh = await setup.TokenBuilder.GenerateTokenAndRefreshTokenAsync("User1");
            context.RefreshTokens.Single().MarkAsInvalid();
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //ATTEMPT
            var tokensAndStatus = await setup.TokenBuilder.RefreshTokenUsingRefreshTokenAsync(tokenAndRefresh);

            //VERIFY
            context.ChangeTracker.Clear();
            tokensAndStatus.HttpStatusCode.ShouldEqual(401);
            setup.Logs.Single().Message.ShouldStartWith("The refresh token in the database was marked as IsInvalid");
        }

        [Fact]
        public async Task RefreshTokenUsingRefreshTokenAsyncRefreshNotInDatabase()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setup = new SetupTokenBuilder(context);
            var tokenAndRefresh = await setup.TokenBuilder.GenerateTokenAndRefreshTokenAsync("User1");
            context.Remove(context.RefreshTokens.Single());
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //ATTEMPT
            var tokensAndStatus = await setup.TokenBuilder.RefreshTokenUsingRefreshTokenAsync(tokenAndRefresh);

            //VERIFY
            context.ChangeTracker.Clear();
            tokensAndStatus.HttpStatusCode.ShouldEqual(400);
            setup.Logs.Single().Message.ShouldStartWith("No refresh token was found in the database.");
        }

        [Fact]
        public async Task RefreshTokenUsingRefreshTokenAsyncRefreshHasExpired()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setup = new SetupTokenBuilder(context, new TimeSpan(0,0,1));
            var tokenAndRefresh = await setup.TokenBuilder.GenerateTokenAndRefreshTokenAsync("User1");
            context.SaveChanges();
            await Task.Delay(1000);

            context.ChangeTracker.Clear();

            //ATTEMPT
            var tokensAndStatus = await setup.TokenBuilder.RefreshTokenUsingRefreshTokenAsync(tokenAndRefresh);

            //VERIFY
            context.ChangeTracker.Clear();
            tokensAndStatus.HttpStatusCode.ShouldEqual(401);
            setup.Logs.Single().Message.ShouldStartWith("Refresh token had expired by");
        }


    }
}