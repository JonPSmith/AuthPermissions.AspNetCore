using System;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode.DataLayer;
using Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;
using Example2.WebApiWithToken.IndividualAccounts.Models;
using Example2.WebApiWithToken.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode;
using Example2.WebApiWithToken.IndividualAccounts.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.DistributedFileStoreCache;
using RunMethodsSequentially;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDefaultIdentity<IdentityUser>(
        options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();


// Configure Authentication using JWT token with refresh capability
var jwtData = new JwtSetupData();
builder.Configuration.Bind("JwtData", jwtData);
//The solution to getting the nameidentifier claim to have the user's Id was found in https://stackoverflow.com/a/70315108/1434764
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(auth =>
{
    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    auth.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtData.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtData.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtData.SigningKey)),
            ClockSkew = TimeSpan.Zero //The default is 5 minutes, but we want a quick expires for JTW refresh

        };

        //This code came from https://www.blinkingcaret.com/2018/05/30/refresh-tokens-in-asp-net-core-web-api/
        //It returns a useful header if the JWT Token has expired
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.RegisterAuthPermissions<Example2Permissions>(options =>
{
    //This tells AuthP that you don't have multiple instances of your app running,
    //so it can run the startup services without a global lock
    options.UseLocksToUpdateGlobalResources = false;

    //This sets up the JWT Token. The config is suitable for using the Refresh Token with your JWT Token
    options.ConfigureAuthPJwtToken = new AuthPJwtConfiguration
    {
        Issuer = jwtData.Issuer,
        Audience = jwtData.Audience,
        SigningKey = jwtData.SigningKey,
        TokenExpires = new TimeSpan(0, 5, 0), //Quick Token expiration because we use a refresh token
        RefreshTokenExpires = new TimeSpan(1, 0, 0, 0) //Refresh token is valid for one day
    };
})
    .UsingEfCoreSqlServer(connectionString) //NOTE: This uses the same database as the individual accounts DB
    .IndividualAccountsAuthentication()
    .AddSuperUserToIndividualAccounts()
    .RegisterFindUserInfoService<IndividualAccountUserLookup>()
    .AddRolesPermissionsIfEmpty(AppAuthSetupData.RolesDefinition)
    .AddAuthUsersIfEmpty(AppAuthSetupData.UsersRolesDefinition)
    .SetupAspNetCoreAndDatabase(options =>
    {
        //Migrate individual account database
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
        //Add demo users to the database
        options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();
    });

builder.Services.AddControllers();

//Register code for updating user's permissions claim when the user's Roles have changed
builder.Services.AddDistributedFileStoreCache(options =>
{
    options.WhichVersion = FileStoreCacheVersions.Class;
    //makes it easier to look at the content, but makes a update very slightly slower 
    options.JsonSerializerForCacheFile = new JsonSerializerOptions { WriteIndented = true };
    //I override the the default first part of the FileStore cache file because there are many example apps in this repo
    options.FirstPartOfCacheFileName = "Example2CacheFileStore";
}, builder.Environment);
builder.Services.AddScoped<IDatabaseStateChangeEvent, RoleChangedDetectorService>();
builder.Services.AddScoped<IDatabaseStateChangeEvent, EmailChangeDetectorService>();

//thanks to: https://www.c-sharpcorner.com/article/authentication-and-authorization-in-asp-net-5-with-jwt-and-swagger/
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Example2.WebApiWithToken.IndividualAccounts", Version = "v1" });

    var securitySchema = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securitySchema);

    var securityRequirement = new OpenApiSecurityRequirement
                {
                    { securitySchema, new[] { "Bearer" } }
                };

    c.AddSecurityRequirement(securityRequirement);
});

var app = builder.Build();

//WARNING: To make this example easy to use I wipe the FileStore cache on startup.
//In normal use you might not want to do this. 
var fsCache = app.Services.GetRequiredService<IDistributedFileStoreCacheClass>();
fsCache.ClearAll();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Example2.WebApiWithToken.IndividualAccounts v1"));
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UsePermissionsChange();   //Example of updating the user's Permission claim when the database change in app using JWT Token for Authentication / Authorization
app.UseAddEmailClaimToUsers();//Example of adding an extra Email 

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
