// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example7.SingleLevelShardingOnly.AppStart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Example7.SingleLevelShardingOnly.EfCoreCode
{
    public class ShardingSingleDesignTimeContextFactory : IDesignTimeDbContextFactory<ShardingOnlyDbContext>          
    {
        // This connection links to an invalidate database, but that's OK as I only used the Add-Migration command
        private const string connectionString =
            "Server=(localdb)\\mssqllocaldb;Database=AuthPermissions;Trusted_Connection=True;MultipleActiveResultSets=true";

        public ShardingOnlyDbContext CreateDbContext(string[] args)   
        {
            var optionsBuilder =                              
                new DbContextOptionsBuilder<ShardingOnlyDbContext>(); 
            optionsBuilder.UseSqlServer(connectionString, dbOptions =>
                dbOptions.MigrationsHistoryTable(StartupExtensions.ShardingSingleDbContextHistoryName));    

            return new ShardingOnlyDbContext(optionsBuilder.Options, null); 
        }
    }
    /******************************************************************************
    * NOTES ON MIGRATION:
    *
    * The AuthPermissionsDbContext is stored in the AuthPermissions project
    * 
    * see https://docs.microsoft.com/en-us/aspnet/core/data/ef-rp/migrations?tabs=visual-studio
    * 
    * Add the following NuGet libraries to this project
    * 1. "Microsoft.EntityFrameworkCore.Tools"
    * 2. "Microsoft.EntityFrameworkCore.SqlServer" (or another database provider)
    * 
    * 2. Using Package Manager Console commands
    * The steps are:
    * a) Make sure the default project is Example7.SingleLevelShardingOnly
    * b) Set the Example7.MvcWebApp.ShardingOnly project as the startup project
    * b) Use the PMC command
    *    Add-Migration Initial -Context ShardingOnlyDbContext -OutputDir EfCoreCode/Migrations
    * c) Don't migrate the database using the Update-database, but use the AddDatabaseOnStartup extension
    *    method when registering the AuthPermissions in ASP.NET Core.
    *    
    * If you want to start afresh then:
    * a) Delete the current database
    * b) Delete all the class in the Migration directory
    * c) follow the steps to add a migration
    ******************************************************************************/
}