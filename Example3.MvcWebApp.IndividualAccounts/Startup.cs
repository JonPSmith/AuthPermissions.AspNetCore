using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.SetupCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreCode;
using Example3.MvcWebApp.IndividualAccounts.Data;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RunMethodsSequentially;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using Example3.InvoiceCode.Services;
using ExamplesCommonCode.IdentityCookieCode;

namespace Example3.MvcWebApp.IndividualAccounts
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        //thanks to https://stackoverflow.com/questions/52040742/get-wwwroot-path-when-in-configureservices-aspnetcore
        //For net6 ASP.NET Core version see https://github.com/JonPSmith/RunStartupMethodsSequentially#for-aspnet-core 
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<IdentityUser>(options =>
                    options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();
            services.ConfigureApplicationCookie(options =>
            {
                //this will cause all the logged-in users to have their claims periodically updated
                options.Events.OnValidatePrincipal = PeriodicCookieEvent.PeriodicRefreshUsersClaims;
            });


            services.RegisterAuthPermissions<Example3Permissions>(options =>
                {
                    options.TenantType = TenantTypes.SingleLevel;
                    options.LinkToTenantType = LinkToTenantTypes.OnlyAppUsers;
                    options.EncryptionKey = _configuration[nameof(AuthPermissionsOptions.EncryptionKey)];
                    options.PathToFolderToLock = _env.WebRootPath;
                })
                //NOTE: This uses the same database as the individual accounts DB
                .UsingEfCoreSqlServer(connectionString)
                .IndividualAccountsAuthentication()
                .RegisterAddClaimToUser<AddTenantNameClaim>()
                .RegisterAddClaimToUser<AddRefreshEveryMinuteClaim>()
                .RegisterTenantChangeService<InvoiceTenantChangeService>()
                .AddRolesPermissionsIfEmpty(Example3AppAuthSetupData.RolesDefinition)
                .AddTenantsIfEmpty(Example3AppAuthSetupData.TenantDefinition)
                .AddAuthUsersIfEmpty(Example3AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase(options =>
                {
                    //Migrate individual account database
                    options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
                    //Add demo users to the database (if no individual account exist)
                    options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();

                    //Migrate the application part of the database
                    options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<InvoicesDbContext>>();
                    //This seeds the invoice database (if empty)
                    options.RegisterServiceToRunInJob<StartupServiceSeedInvoiceDbContext>();
                });

            services.RegisterExample3Invoices(_configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
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
