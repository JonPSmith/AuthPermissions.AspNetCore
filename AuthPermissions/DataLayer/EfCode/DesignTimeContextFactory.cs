// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.PermissionsCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthPermissions.DataLayer.EfCode
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<AuthPermissionsDbContext>          
    {
        private const string connectionString =
            "Server=(localdb)\\mssqllocaldb;Database=AuthPermissions;Trusted_Connection=True;MultipleActiveResultSets=true";

        public AuthPermissionsDbContext CreateDbContext(string[] args)   
        {
            var optionsBuilder =                              
                new DbContextOptionsBuilder<AuthPermissionsDbContext>(); 
            optionsBuilder.UseSqlServer(connectionString, dbOptions =>
                dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));    

            return new AuthPermissionsDbContext(optionsBuilder.Options); 
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
    * a) Make sure the default project is AuthPermissions
    * b) Use the PMC command
    *    Add-Migration NameForMigration -Context AuthPermissionsDbContext -OutputDir DataLayer/Migrations
    * c) Don't migrate the database using the Update-database, but use the AddDatabaseOnStartup extension
    *    method when registering the AuthPermissions in ASP.NET Core.
    *    
    * If you want to start afresh then:
    * a) Delete the current database
    * b) Delete all the class in the Migration directory
    * c) follow the steps to add a migration
    ******************************************************************************/
}