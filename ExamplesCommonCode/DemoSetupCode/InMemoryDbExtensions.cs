// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExamplesCommonCode.DemoSetupCode
{
    public static class InMemoryDbExtensions
    {
        public static void RegisterInMemoryDb<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            var connection = SetupSqliteInMemoryConnection();
            services.AddDbContext<TContext>(options => options.UseSqlite(connection));
        }

        public static SqliteConnection SetupSqliteInMemoryConnection()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();  //see https://github.com/aspnet/EntityFramework/issues/6968
            return connection;
        }
    }
}