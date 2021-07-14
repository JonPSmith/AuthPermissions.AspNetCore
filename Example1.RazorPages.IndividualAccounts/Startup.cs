using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using Example1.RazorPages.IndividualAccounts.Data;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.DemoSetupCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Example1.RazorPages.IndividualAccounts
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
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseInMemoryDatabase(nameof(ApplicationDbContext)));

            services.AddDefaultIdentity<IdentityUser>(
                    options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            //Example of configure a page as only shown if you log in
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/AuthBuiltIn/LoggedInConfigure");
            });

            //These are methods from the ExamplesCommonCode set up some demo users in the individual accounts database
            //NOTE: they are run in the order that they are registered
            services.AddHostedService<HostedServiceEnsureCreatedDb<ApplicationDbContext>>(); //and create db on startup
            services.AddHostedService<HostedServiceAddAspNetUsers>(); //reads a comma delimited list of emails from appsettings.json

            services.RegisterAuthPermissions<Example1Permissions>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(AppAuthSetupData.ListOfRolesWithPermissions)
                .AddUsersRolesIfEmpty(AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .IndividualAccountsAddSuperUserIfNoUsers()
                .SetupAuthDatabaseOnStartup();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
