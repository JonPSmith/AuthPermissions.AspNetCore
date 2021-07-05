// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core MVC";
        public string AuthorizationProvider { get; } = "ASP.NET Core's individual accounts";
        public string CookieOrToken { get; } = "Cookie";
        public string MultiTenant { get; } = "Hierarchical multi-tenant";
        public string[] Databases { get; } = new []
        {
            "One SQL Server database shared by:",
            "- ASP.NET Core Individual accounts database",
            "- AuthPermissions' database",
            "- multi-tenant database"
        };
        public string Note { get; } =  "This is more like a real application, with lots of users and roles/permissions. " +
                                       "It also has a hierarchical multi-tenant setup and a AuthUsers controller using AuthPermissions' AuthUsersAdminService.";
    }
}