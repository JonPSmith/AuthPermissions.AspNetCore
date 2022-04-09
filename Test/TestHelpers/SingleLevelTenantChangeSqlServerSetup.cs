// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using TestSupport.Helpers;

namespace Test.TestHelpers
{
    public class SingleLevelTenantChangeSqlServerSetup : IDisposable
    {
        public AuthPermissionsDbContext AuthPContext { get; }

        public InvoicesDbContext InvoiceDbContext { get; }


        public SingleLevelTenantChangeSqlServerSetup(object caller)
        {
            var authOptions = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
                .UseSqlServer(caller.GetUniqueDatabaseConnectionString("authp"), dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));
            EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions.UseExceptionProcessor(authOptions);
            AuthPContext = new AuthPermissionsDbContext(authOptions.Options);

            var retailOptions = new DbContextOptionsBuilder<InvoicesDbContext>()
                .UseSqlServer(caller.GetUniqueDatabaseConnectionString("invoice"), dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.InvoicesDbContextHistoryName)).Options;
            InvoiceDbContext = new InvoicesDbContext(retailOptions, null);

            AuthPContext.Database.EnsureClean();
            InvoiceDbContext.Database.EnsureClean();
        }

        public void Dispose()
        {
            AuthPContext?.Dispose();
            InvoiceDbContext?.Dispose();
        }
    }
}