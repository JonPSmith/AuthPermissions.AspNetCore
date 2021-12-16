using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.OpenIdCode;
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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(_configuration.GetSection("AzureAd"));

            services.AddControllersWithViews();
            services.AddRazorPages()
                 .AddMicrosoftIdentityUI();

            //Needed by the SyncAzureAdUsers code
            services.Configure<AzureAdOptions>(_configuration.GetSection("AzureAd"));

            services.RegisterAuthPermissions<Example5Permissions>(options =>
                {
                    options.AppConnectionString = connectionString;
                    options.PathToFolderToLock = _env.WebRootPath;
                })
                .AzureAdAuthentication(AzureAdSettings.AzureAdDefaultSettings(false))
                .UsingEfCoreSqlServer(_configuration.GetConnectionString("DefaultConnection"))
                .AddRolesPermissionsIfEmpty(Example5AppAuthSetupData.RolesDefinition)
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
