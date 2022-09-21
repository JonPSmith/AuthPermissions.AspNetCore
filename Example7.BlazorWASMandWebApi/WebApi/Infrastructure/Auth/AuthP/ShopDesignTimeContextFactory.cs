using Example7.BlazorWASMandWebApi.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Auth.AuthP;

public class ShopDesignTimeContextFactory : IDesignTimeDbContextFactory<RetailDbContext>
{
    // This connection links to an invalidate database, but that's OK as I only used the Add-Migration command
    private const string connectionString =
        "Server=(localdb)\\mssqllocaldb;Database=AuthPermissions;Trusted_Connection=True;MultipleActiveResultSets=true";

    public RetailDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<RetailDbContext>();
        optionsBuilder.UseSqlServer(connectionString, dbOptions =>
            dbOptions.MigrationsHistoryTable(StartupExtensions.RetailDbContextHistoryName));

        return new RetailDbContext(optionsBuilder.Options, null);
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
* a) Make sure the default project is Example7.BlazorWASMandWebApi.Infrastructure
* b) Set the Example7 project as the startup project
* b) Use the PMC command
*    Add-Migration Initial -Context RetailDbContext -OutputDir EfCoreCode/Migrations
* c) Don't migrate the database using the Update-database, but use the AddDatabaseOnStartup extension
*    method when registering the AuthPermissions in ASP.NET Core.
*    
* If you want to start afresh then:
* a) Delete the current database
* b) Delete all the class in the Migration directory
* c) follow the steps to add a migration
******************************************************************************/

