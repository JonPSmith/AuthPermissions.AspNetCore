using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program
{
    //Code taken from https://github.com/dotnet/EntityFramework.Docs/tree/main/samples/core/Schemas/TwoProjectMigrations/WorkerService1
    //See document https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers
    private static void Main(string[] args)
        => CreateHostBuilder(args).Build().Run();

    //The databases these aren't used - you just need a valid connection string
    private const string SqlServerConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=DummyDatabase;Trusted_Connection=True";
    private const string PostgresConnectionString =
        "host=127.0.0.1;Database=DummyDatabase;Username=postgres;Password=LetMeIn";

    public static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (hostContext, services) =>
                {
                    // Set the active provider via configuration
                    var configuration = hostContext.Configuration; 
                    var provider = configuration.GetValue("Provider", "No provider set");

                    services.AddDbContext<AuthPermissionsDbContext>(
                        options => _ = provider switch
                        {
                            "SqlServer" => options.UseSqlServer(SqlServerConnectionString,
                                x => x.MigrationsAssembly("AuthPermissions.SqlServer")),

                            "PostgreSql" => options.UseNpgsql(PostgresConnectionString,
                                x => x.MigrationsAssembly("AuthPermissions.PostgreSql")),
                            
                            _ => throw new Exception($"Unsupported provider: {provider}")
                        });
                });
}
/******************************************************************************
* NOTES ON MIGRATION:
*
* The AuthPermissionsDbContext, which can be found in the AuthPermissions project, is designed
* to work with either SQL Server and Postgres. To do this you have to:
 * 1. Make sure the AuthPermissionsDbContext can configure the database for both SQL Server and Postgres
 * 2. Create a AuthPermissions.SqlServer and AuthPermissions.PostgreSql project to contain each migrations
 * 3. Build this Generic Host (taken from)
 *    https://github.com/dotnet/EntityFramework.Docs/tree/main/samples/core/Schemas/TwoProjectMigrations/WorkerService1
* 
* This described in https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers
* 
* 
* 2. Use Console command
* The steps are:
* a) Make sure the default project is SetupMigrations
* b) Use a Console command with the following commands
*   
*   cd C:\Users\JonPSmith\source\repos\AuthPermissions.AspNetCore\SetupMigrations
*
* For PostgreSql
*   dotnet ef migrations add Version3 --project ../AuthPermissions.PostgreSql -- --provider PostgreSql
*
* For SqlServer
*   dotnet ef migrations add Version3 --project ../AuthPermissions.SqlServer -- --provider SqlServer
*
* If you want to start afresh then:
* a) Delete the current database
* b) Delete all the class in the Migration directories 
* c) follow the steps to add a migration
******************************************************************************/