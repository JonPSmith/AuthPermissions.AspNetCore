// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.Classes.SupportTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthPermissions.DataLayer.EfCode
{
    /// <summary>
    /// DesignTimeDbContextFactory to allow migration to be created for the <see cref="AuthPermissionsDbContext"/>
    /// </summary>
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<AuthPermissionsDbContext>
    {
        private const string connectionString =
            "Server=(localdb)\\mssqllocaldb;Database=aspnet-Example4.MvcWebApp.IndividualAccounts-39EF2337-4CA7-4EA1-8FC5-2344A6027538;Trusted_Connection=True;MultipleActiveResultSets=true";


        /// <summary>
        /// Create the AuthPermissionsDbContext
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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
    *    Add-Migration Initial -Context AuthPermissionsDbContext -OutputDir DataLayer/Migrations
    * c) Don't migrate the database using the Update-database, but use the AddDatabaseOnStartup extension
    *    method when registering the AuthPermissions in ASP.NET Core.
    *    
    * If you want to start afresh then:
    * a) Delete the current database
    * b) Delete all the class in the Migration directory
    * c) follow the steps to add a migration
    ******************************************************************************/
}