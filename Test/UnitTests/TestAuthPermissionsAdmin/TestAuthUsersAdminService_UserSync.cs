// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestAuthUsersAdminService_UserSync
    {
        private readonly AuthPermissionsOptions _authOptionsSingle =
            new() { TenantType = TenantTypes.SingleLevel };

        private readonly ITestOutputHelper _output;

        public TestAuthUsersAdminService_UserSync(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestSyncAndShowChangesAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var authenticationServiceFactory = new StubSyncAuthenticationUsersFactory();
            var service = new AuthUsersAdminService(context, authenticationServiceFactory, 
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var changes = await service.SyncAndShowChangesAsync();

            //VERIFY
            foreach (var synChange in changes)
            {
                _output.WriteLine(synChange.ToString());
            }
            changes.Select(x => x.FoundChangeType.ToString()).ShouldEqual(new []{ "Update", "Create", "Delete" });
            changes.Select(x => x.ToString()).ShouldEqual(new[]
            {
                "UPDATE: Email CHANGED, UserName CHANGED",
                "CREATE: Email = user99@gmail.com, UserName = user 99",
                "DELETE: Email = user3@gmail.com, UserName = first last 2"
            });
        }

        [Fact]
        public async Task TestSyncAndShowChangesAsyncCheckEmailAndUserName()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var authenticationServiceFactory = new StubSyncAuthenticationUsersFactory();
            var service = new AuthUsersAdminService(context, authenticationServiceFactory,
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer());

            //ATTEMPT
            var changes = await service.SyncAndShowChangesAsync();

            //VERIFY
            foreach (var synChange in changes)
            {
                _output.WriteLine(synChange.ToString());
            }
            changes.Select(x => x.FoundChangeType.ToString()).ShouldEqual(new[] { "Update", "Create", "Delete" });
            changes.Select(x => x.Email).ShouldEqual(new[] { "user2@newgmail.com", "user99@gmail.com", "user3@gmail.com" });
            changes.Select(x => x.UserName).ShouldEqual(new[] { "new name", "user 99", "first last 2" });
            changes.Select(x => x.ToString()).ShouldEqual(new[]
            {
                "UPDATE: Email CHANGED, UserName CHANGED",
                "CREATE: Email = user99@gmail.com, UserName = user 99",
                "DELETE: Email = user3@gmail.com, UserName = first last 2"
            });
        }

        [Fact]
        public async Task TestApplySyncChangesAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupRolesInDbAsync();
            context.AddMultipleUsersWithRolesInDb();
            context.ChangeTracker.Clear();

            var authenticationServiceFactory = new StubSyncAuthenticationUsersFactory();
            var service = new AuthUsersAdminService(context, authenticationServiceFactory,
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer());
            var changes = await service.SyncAndShowChangesAsync();

            //ATTEMPT
            var status = await service.ApplySyncChangesAsync(changes);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            _output.WriteLine(status.Message);
            context.ChangeTracker.Clear();
            var authUsers = context.AuthUsers.OrderBy(x => x.Email).ToList();
            foreach (var authUser in authUsers)
            {
                _output.WriteLine(authUser.ToString());
            }
            authUsers.Select(x => x.Email).ShouldEqual(new []{ "user1@gmail.com", "user2@newgmail.com", "user99@gmail.com" });
            authUsers.Select(x => x.UserName).ShouldEqual(new[] { "first last 0", "new name", "user 99" });
        }

    }
}