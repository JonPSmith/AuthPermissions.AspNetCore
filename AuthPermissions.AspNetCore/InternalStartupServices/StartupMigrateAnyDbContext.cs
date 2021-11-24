// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore.InternalStartupServices
{
    /// <summary>
    /// This will run EF Core's Migate method on the given DbContext
    /// Note that if the database is an in-memory, then it will simply create it
    /// </summary>
    internal class StartupMigrateAnyDbContext<TContext> where TContext : DbContext
    {
        /// <summary>
        /// This migrates the given <typeparamref name="TContext"/> DbContext
        /// </summary>
        /// <param name="scopedService">This should be a scoped service</param>
        /// <returns></returns>
        public async ValueTask StartupServiceAsync(IServiceProvider scopedService)
        {
            var context = scopedService.GetRequiredService<TContext>();

            if (context.Database.IsInMemory())
                await context.Database.EnsureCreatedAsync();
            else
                await context.Database.MigrateAsync();
        }
    }
}