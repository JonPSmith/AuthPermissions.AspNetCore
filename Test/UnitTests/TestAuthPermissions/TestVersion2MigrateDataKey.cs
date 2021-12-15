// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions;

public class TestVersion2MigrateDataKey
{
    private ITestOutputHelper _output;

    public TestVersion2MigrateDataKey(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestMigrateVersion1AuthPermissionsDbContext()
    {
        //SETUP
        var connectionString = AppSettings.GetConfiguration().GetConnectionString("Version1Example4");
        var builder = new DbContextOptionsBuilder<AuthPermissionsDbContext>();
        builder.UseSqlServer(connectionString);
        using var context = new AuthPermissionsDbContext(builder.Options);

        using var transaction = context.Database.BeginTransaction();
        foreach (var tenant in context.Tenants)
        {
            _output.WriteLine($"{tenant.ParentDataKey}, \t {tenant.TenantFullName}");
        }

        //ATTEMPT
        var sql = "authp.Tenants".CreateVersion2DataKeyUpdateSql("ParentDataKey");
        context.Database.ExecuteSqlRaw(sql);

        //VERIFY
        context.ChangeTracker.Clear();
        var updatedTenants = context.Tenants.ToList();
        _output.WriteLine("------------------------------------------");
        foreach (var tenant in updatedTenants)
        {
            _output.WriteLine($"{tenant.ParentDataKey}, \t {tenant.TenantFullName}");
        }
        updatedTenants.Where(x => x.ParentDataKey != null).All(x => x.ParentDataKey[0] != '.').ShouldBeTrue();
    }
}