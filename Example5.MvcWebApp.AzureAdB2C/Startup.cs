using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.SetupCode;
using Example5.MvcWebApp.AzureAdB2C.AzureAdCode;
using Example5.MvcWebApp.AzureAdB2C.PermissionCode;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace Example5.MvcWebApp.AzureAdB2C
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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"));

            services.AddControllersWithViews(options =>
            {
                //var policy = new AuthorizationPolicyBuilder()
                //    .RequireAuthenticatedUser()
                //    .Build();
                //options.Filters.Add(new AuthorizeFilter(policy));
            });
            services.AddRazorPages()
                 .AddMicrosoftIdentityUI();

            //Needed by the SyncAzureAdUsers code
            services.Configure<AzureAdOptions>(Configuration.GetSection("AzureAd"));

            services.RegisterAuthPermissions<Example5Permissions>()
                .AzureAdAuthentication(AzureAdSettings.AzureAdDefaultSettings(false))
                .UsingEfCoreSqlServer(Configuration.GetConnectionString("DefaultConnection"))
                .AddRolesPermissionsIfEmpty(Example5AppAuthSetupData.BulkLoadRolesWithPermissions)
                .AddAuthUsersIfEmpty(Example5AppAuthSetupData.UsersRolesDefinition)
                .RegisterAuthenticationProviderReader<SyncAzureAdUsers>()
                .SetupAspNetCoreAndDatabase();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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
