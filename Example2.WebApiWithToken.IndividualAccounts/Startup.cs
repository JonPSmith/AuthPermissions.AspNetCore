using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using Example2.WebApiWithToken.IndividualAccounts.Data;
using Example2.WebApiWithToken.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.DemoSetupCode;
using Microsoft.EntityFrameworkCore;

namespace Example2.WebApiWithToken.IndividualAccounts
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            services.AddDefaultIdentity<IdentityUser>(
                    options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            //The HostedServices to run at startup
            //NOTE: they are run in the order that they are registered
            services.AddHostedService<HostedServiceEnsureCreatedDb<ApplicationDbContext>>(); //and create db on startup
            services.AddHostedService<HostedServiceAddAspNetUsers>(); //reads a comma delimited list of emails from appsettings.json

            services.RegisterAuthPermissions<Example2Permissions>()
                .UsingEfCoreSqlServer(connectionString) //NOTE: This uses the same database as the individual accounts DB
                .AddRolesPermissionsIfEmpty(AppAuthSetupData.ListOfRolesWithPermissions)
                .AddUsersRolesIfEmptyWithUserIdLookup<IndividualUserUserLookup>(AppAuthSetupData.UsersRolesDefinition)
                .IndividualAccountsAddSuperUser()
                .SetupAuthDatabaseOnStartup();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Example2.WebApiWithToken.IndividualAccounts", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Example2.WebApiWithToken.IndividualAccounts v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
