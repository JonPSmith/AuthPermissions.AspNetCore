// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using TestSupport.Attributes;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample4SetupAuthPermissions
    {
        private readonly ITestOutputHelper _output;

        public TestExample4SetupAuthPermissions(ITestOutputHelper output)
        {
            _output = output;
        }

        [RunnableInDebugOnly]
        public void CreateDemoUsersString()
        {
            //SETUP

            //ATTEMPT
            var userEmails = Example4AppAuthSetupData.UsersRolesDefinition
                .Select(x => x.Email)
                .Where(x => x != "Super@g1.com");

            //VERIFY
            _output.WriteLine(string.Join(", ", userEmails));
        }

        [Fact]
        public async Task TestExample4TenantsLoad()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new BulkLoadTenantsService(context);

            //ATTEMPT
            var status = await service.AddTenantsToDatabaseAsync(Example4AppAuthSetupData.TenantDefinition, 
                new AuthPermissionsOptions{TenantType = TenantTypes.HierarchicalTenant});

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            foreach (var tenant in context.Tenants)
            {
                _output.WriteLine(tenant.ToString());
            }
        }


    }
}