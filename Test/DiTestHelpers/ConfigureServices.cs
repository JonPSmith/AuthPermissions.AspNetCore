// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestSupport.Helpers;

namespace Test.DiTestHelpers
{
    public static class ConfigureServices
    {
        public static ServiceCollection SetupServicesForTest(this object callingClass, bool useSqlDbs = false)
        {
            var services = new ServiceCollection();
            services.RegisterDatabases(callingClass, useSqlDbs);

            //Wanted to use the line below but just couldn't get the right package for it
            //services.AddDefaultIdentity<IdentityUser>()
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            var startupConfig = AppSettings.GetConfiguration();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(startupConfig);

            //make sure the  databases are created
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();

            return services;
        }

        public static ServiceCollection SetupServicesForTestCustomIdentityUser(this object callingClass, bool useSqlDbs = false)
        {
            var services = new ServiceCollection();
            services.RegisterCustomDatabases(callingClass, useSqlDbs);

            //Wanted to use the line below but just couldn't get the right package for it
            //services.AddDefaultIdentity<IdentityUser>()
            services.AddIdentity<CustomIdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<CustomApplicationDbContext>();
            var startupConfig = AppSettings.GetConfiguration();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(startupConfig);

            //make sure the  databases are created
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<CustomApplicationDbContext>().Database.EnsureCreated();

            return services;
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

        private static void RegisterCustomDatabases(this ServiceCollection services, object callingClass, bool useSqlDbs)
        {
            if (useSqlDbs)
            {
                var aspNetConnectionString = callingClass.GetUniqueDatabaseConnectionString("AspNet");
                services.AddDbContext<CustomApplicationDbContext>(options => options.UseSqlServer(aspNetConnectionString));

                var appConnectionString = callingClass.GetUniqueDatabaseConnectionString("AppData");
            }
            else
            {

                var aspNetAuthConnection = SetupSqliteInMemoryConnection();
                services.AddDbContext<CustomApplicationDbContext>(options => options.UseSqlite(aspNetAuthConnection));
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