// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example6.SingleLevelSharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCore.AutoRegisterDi;

namespace Example6.SingleLevelSharding.AppStart
{
    public static class StartupExtensions
    {
        public const string ShardingSingleDbContextHistoryName = "Example6-ShardingSingleDbContext";

        public static void RegisterExample6Invoices(this IServiceCollection services, IConfiguration configuration)
        {
            //Register any services in this project
            services.RegisterAssemblyPublicNonGenericClasses()
                .Where(c => c.Name.EndsWith("Service"))  //optional
                .AsPublicImplementedInterfaces();

            //Register the retail database to the same database used for individual accounts and AuthP database
            services.AddDbContext<ShardingSingleDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), dbOptions =>
                dbOptions.MigrationsHistoryTable(ShardingSingleDbContextHistoryName)));
        }
    }
}