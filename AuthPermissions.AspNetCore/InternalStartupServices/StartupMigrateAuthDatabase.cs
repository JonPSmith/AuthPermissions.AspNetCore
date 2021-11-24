// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthPermissions.AspNetCore.InternalStartupServices
{
    /// <summary>
    /// This will run before ASP.NET Core goes live, and migrates 
    /// </summary>
    internal class StartupMigrateAuthDatabase
    {
        /// <summary>
        /// This will migrate the AuthP's database on startup
        /// </summary>
        /// <returns></returns>
        public async ValueTask StartupServiceAsync(IServiceProvider scopedService)
        {
            var context = scopedService.GetRequiredService<AuthPermissionsDbContext>();
            var options = scopedService.GetRequiredService<AuthPermissionsOptions>();
            try
            {
                if (options.InternalData.AuthPDatabaseType == AuthPDatabaseTypes.SqliteInMemory)
                    throw new AuthPermissionsException(
                        $"The in-memory database is created by the {nameof(AuthPermissions.SetupExtensions.UsingInMemoryDatabase)} extension method");
                else
                    await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                var logger = scopedService.GetRequiredService<ILogger<StartupMigrateAuthDatabase>>();
                logger.LogError(ex, "An error occurred while creating/migrating the SQL database.");

                throw;
            }
        }
    }
}