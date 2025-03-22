// Copyright (c) 2024 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using AuthPermissions.BaseCode.DataLayer.EfCode;

namespace AuthPermissions.PostgreSql;

public class AuthPermissionsDbContextDesignTimeContextFactory : IDesignTimeDbContextFactory<AuthPermissionsDbContext>
{
    // This connection links to an invalidate database, but that's OK as I only used the Add-Migration command
    private const string connectionString =
        "Host=127.0.0.1;Port=5432;Database=NoDb;Username=postgres;Password=LetMeIn";

    public AuthPermissionsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<AuthPermissionsDbContext>();
        optionsBuilder.UseNpgsql(connectionString, 
            b => b.MigrationsAssembly("AuthPermissions.PostgreSql"));

        return new AuthPermissionsDbContext(optionsBuilder.Options);
    }

    /******************************************************************************
     * NOTES ON MIGRATION:
     *
     * The migration of the PostgreSql version of AuthPermissionsDbContext is a bit different to the SqlServer version.
     * You MUST have a IDesignTimeDbContextFactory<AuthPermissionsDbContext> for PostgreSql (SqlServer doesn't need that)
     *
     * see https://docs.microsoft.com/en-us/aspnet/core/data/ef-rp/migrations?tabs=visual-studio
     *
     * Add the following NuGet libraries to this project
     * 1. "Microsoft.EntityFrameworkCore.Tools"
     * 2. "Microsoft.EntityFrameworkCore.PostgreSQL"
     * 3. "Microsoft.EntityFrameworkCore.Design"
     *
     * 2. Using Package Manager Console commands
     * The steps are:
     * a) Make sure the default project, e.g. "set as Startup Project", is AuthPermissions.PostgreSql
     * b) Set the PMC project to be AuthPermissions.PostgreSql
     * b) Use the PMC command
     *    Add-Migration Initial -Context AuthPermissionsDbContext
     *
     ******************************************************************************/
}
