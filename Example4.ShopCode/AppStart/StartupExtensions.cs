// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Reflection;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.CommonCode;
using Example4.ShopCode.EfCoreCode;
using ExamplesCommonCode.DemoSetupCode;
using GenericServices.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example4.ShopCode.AppStart
{
    public static class StartupExtensions
    {
        public const string RetailDbContextHistoryName = "Example4-RetailDbContext";

        public static void RegisterExample4ShopCode(this IServiceCollection services, IConfiguration configuration)
        {
            //Register the retail database to the same database used for individual accounts and AuthP database
            services.AddDbContext<RetailDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), dbOptions =>
                dbOptions.MigrationsHistoryTable(RetailDbContextHistoryName)));

            //------------------------------------------------------
            //Hosted services

            //This will migrate the RetailDbContext on startup (WARNING: Only works for single instance of the ASP.NET Core app)
            services.AddHostedService<HostedServiceEnsureCreatedDb<RetailDbContext>>();
            //This will seed the retail database if no RetailOutlets are there
            services.AddHostedService<HostedServiceSeedRetailDatabase>();


        }
    }
}