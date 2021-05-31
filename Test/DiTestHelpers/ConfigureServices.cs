// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestSupport.Helpers;

namespace Test.DiTestHelpers
{
    public static class ConfigureServices
    {
        public static ServiceProvider SetupServicesForTest(this object callingClass, bool useSqlDbs = false)
        {
            var services = new ServiceCollection();
            services.RegisterDatabases(callingClass, useSqlDbs);

            //Wanted to use the line below but just couldn't get the right package for it
            //services.AddDefaultIdentity<IdentityUser>()
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddLogging();

            var serviceProvider = services.BuildServiceProvider();

            //make sure the  databases are created
            serviceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();

            return serviceProvider;
        }

        private static void RegisterDatabases(this ServiceCollection services, object callingClass, bool useSqlDbs)
        {
            if (useSqlDbs)
            {
                var aspNetConnectionString = callingClass.GetUniqueDatabaseConnectionString("AspNet");
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(aspNetConnectionString));

                var appConnectionString = callingClass.GetUniqueDatabaseConnectionString("AppData");
            }
            else
            {

                var aspNetAuthConnection = SetupSqliteInMemoryConnection();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(aspNetAuthConnection));
                var appExtraConnection = SetupSqliteInMemoryConnection();
            }
        }

        private static SqliteConnection SetupSqliteInMemoryConnection()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();  //see https://github.com/aspnet/EntityFramework/issues/6968
            return connection;
        }
    }
}