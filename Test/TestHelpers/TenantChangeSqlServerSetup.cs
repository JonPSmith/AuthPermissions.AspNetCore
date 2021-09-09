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
    public class TenantChangeSqlServerSetup : IDisposable
    {
        public string ConnectionString { get; }
        public AuthPermissionsDbContext AuthPContext { get; }

        public RetailDbContext RetailDbContext { get; }


        public TenantChangeSqlServerSetup(object caller)
        {
            ConnectionString = caller.GetUniqueDatabaseConnectionString();

            var authOptions = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
                .UseSqlServer(ConnectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));
            EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions.UseExceptionProcessor(authOptions);
            AuthPContext = new AuthPermissionsDbContext(authOptions.Options);

            var retailOptions = new DbContextOptionsBuilder<RetailDbContext>()
                .UseSqlServer(ConnectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.RetailDbContextHistoryName)).Options;
            RetailDbContext = new RetailDbContext(retailOptions, null);

            AuthPContext.Database.EnsureClean(false);

            AuthPContext.Database.Migrate();
            RetailDbContext.Database.Migrate();
        }

        public void Dispose()
        {
            AuthPContext?.Dispose();
            RetailDbContext?.Dispose();
        }
    }
}