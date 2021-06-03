// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.DataKeyCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestCalcDataKey
    {
        [Fact]
        public async Task TestCalcAllowedPermissionsSimple()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenant = new Tenant("Tenant1");
            var userTenant = new UserToTenant("User1", tenant, "User1");
            context.AddRange(tenant, userTenant);
            context.SaveChanges();

            context.ChangeTracker.Clear();
            var authOptions = new AuthPermissionsOptions {TenantType = TenantTypes.SingleTenant};

            var service = new CalcDataKey(context, authOptions);

            //ATTEMPT
            var dataKey = await service.GetDataKeyAsync("User1");

            //VERIFY
            dataKey.ShouldEqual(tenant.TenantDataKey);
        }

    }
}