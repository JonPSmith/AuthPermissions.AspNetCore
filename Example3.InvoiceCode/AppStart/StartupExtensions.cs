// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example3.InvoiceCode.EfCoreCode;
using ExamplesCommonCode.DemoSetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example3.InvoiceCode.AppStart
{
    public static class StartupExtensions
    {
        public const string InvoicesDbContextHistoryName = "Example3-InvoicesDbContext";

        public static void RegisterExample3Invoices(this IServiceCollection services, IConfiguration configuration)
        {
            //Register the retail database to the same database used for individual accounts and AuthP database
            services.AddDbContext<InvoicesDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), dbOptions =>
                dbOptions.MigrationsHistoryTable(InvoicesDbContextHistoryName)));

            //------------------------------------------------------
            //Hosted services

            //This will migrate the RetailDbContext on startup (WARNING: Only works for single instance of the ASP.NET Core app)
            services.AddHostedService<HostedServiceEnsureCreatedDb<InvoicesDbContext>>();
            //This will seed the retail database if no RetailOutlets are there
            services.AddHostedService<HostedServiceSeedInvoiceDatabase>();
        }
    }
}