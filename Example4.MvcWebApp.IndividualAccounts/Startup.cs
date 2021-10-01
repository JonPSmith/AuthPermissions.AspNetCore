using Example4.MvcWebApp.IndividualAccounts.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example4.ShopCode.AppStart;
using Example4.ShopCode.Dtos;
using Example4.ShopCode.EfCoreCode;
using ExamplesCommonCode.DemoSetupCode;
using GenericServices.Setup;

namespace Example4.MvcWebApp.IndividualAccounts
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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<IdentityUser>(options => 
                    options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            //These are methods from the ExamplesCommonCode set up some demo users in the individual accounts database
            //NOTE: they are run in the order that they are registered
            services.AddHostedService<HostedServiceEnsureCreatedDb<ApplicationDbContext>>(); //and create db on startup
            services.AddHostedService<HostedServiceAddAspNetUsers>(); //reads a comma delimited list of emails from appsettings.json

            services.RegisterAuthPermissions<Example4Permissions>(options =>
                {
                    options.TenantType = TenantTypes.HierarchicalTenant;
                    options.AppConnectionString = Configuration.GetConnectionString("DefaultConnection");
                })
                //NOTE: This uses the same database as the individual accounts DB
                .UsingEfCoreSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .UsingIndividualAccounts()
                .RegisterTenantChangeService<RetailTenantChangeService>()
                .AddRolesPermissionsIfEmpty(Example4AppAuthSetupData.BulkLoadRolesWithPermissions)
                .AddTenantsIfEmpty(Example4AppAuthSetupData.BulkHierarchicalTenants)
                .AddAuthUsersIfEmpty(Example4AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            //This registers all the code to handle the shop part of the demo
            //Register RetailDbContext database and some services (included hosted services)
            services.RegisterExample4ShopCode(Configuration);
            //Add GenericServices (after registering the RetailDbContext context
            services.GenericServicesSimpleSetup<RetailDbContext>(Assembly.GetAssembly(typeof(ListSalesDto)));
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
                app.UseExceptionHandler("/Home/Error");
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
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
