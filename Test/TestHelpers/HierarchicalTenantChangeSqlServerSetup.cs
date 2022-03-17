// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using Example4.ShopCode.AppStart;
using Example4.ShopCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using TestSupport.Helpers;

namespace Test.TestHelpers
{
    public class HierarchicalTenantChangeSqlServerSetup : IDisposable
    {
        public AuthPermissionsDbContext AuthPContext { get; }

        public RetailDbContext RetailDbContext { get; }


        public HierarchicalTenantChangeSqlServerSetup(object caller)
        {
            var authOptions = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
                .UseSqlServer(caller.GetUniqueDatabaseConnectionString("authp"), dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));
            EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions.UseExceptionProcessor(authOptions);
            AuthPContext = new AuthPermissionsDbContext(authOptions.Options);

            var retailOptions = new DbContextOptionsBuilder<RetailDbContext>()
                .UseSqlServer(caller.GetUniqueDatabaseConnectionString("retail"), dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.RetailDbContextHistoryName)).Options;
            RetailDbContext = new RetailDbContext(retailOptions, null);

            AuthPContext.Database.EnsureClean();
            RetailDbContext.Database.EnsureClean();
        }

        public void Dispose()
        {
            AuthPContext?.Dispose();
            RetailDbContext?.Dispose();
        }
    }
}