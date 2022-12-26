// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Globalization;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.StartupServices;
using Example1.RazorPages.IndividualAccounts.Data;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using RunMethodsSequentially;
using Microsoft.Extensions.Options;

namespace Example1.RazorPages.IndividualAccounts;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseInMemoryDatabase(nameof(ApplicationDbContext)));

        builder.Services.AddDefaultIdentity<IdentityUser>(
                options => options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<ApplicationDbContext>();

        //Example of configure a page as only shown if you log in
        builder.Services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizePage("/AuthBuiltIn/LoggedInConfigure");
        })
        #region localization
            .AddViewLocalization(options => options.ResourcesPath = "Resources");
        #endregion

        #region localization - defining the cultures 
        //see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization#localization-middleware
        var supportedCultures = new[] { "en", "fr"};
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);


        //This defines that the culture is selected by the culture cookie
        localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>()
        {
            new CookieRequestCultureProvider(),
            //new AcceptLanguageHeaderRequestCultureProvider(),
            //new QueryStringRequestCultureProvider()
        };

        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            List<CultureInfo> supportedCultures = new List<CultureInfo>
            {
                new ("en"),
                new ("fr"),
            };
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.RequestCultureProviders = new List<IRequestCultureProvider>()
            {
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider(),
                new QueryStringRequestCultureProvider()
            };
        });
        #endregion

        //Register the SimpleLocalizer with its own Resource file
        //This is used for localization of simple messages
        builder.Services.RegisterSimpleLocalizer<AppLocalizeResource>();

        builder.Services.RegisterAuthPermissions<Example1Permissions>()
            .UsingInMemoryDatabase()
            .IndividualAccountsAuthentication()
            .AddRolesPermissionsIfEmpty(AppAuthSetupData.RolesDefinition)
            .AddAuthUsersIfEmpty(AppAuthSetupData.UsersWithRolesDefinition)
            .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
            .RegisterFindUserInfoService<IndividualAccountUserLookup>()
            .SetupAuthPLocalization<AuthPLocalizeResource>(supportedCultures)
            .AddSuperUserToIndividualAccounts()
            .SetupAspNetCoreAndDatabase(options =>
            {
                //Migrate individual account database
                options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
                //Add demo users to the database
                options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();
            });


        var app = builder.Build();

        #region localization

        //see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization#localization-middleware
        app.UseRequestLocalization(localizationOptions);
        //var options = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        //app.UseRequestLocalization(options);

        #endregion

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }

}