// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example5.MvcWebApp.AzureAdB2C.Models
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
            "- AuthPermissions' database - tables have a schema of 'authp'",
            "- multi-tenant retail database - tables have a schema of 'retail'"
        };
        public string Note { get; } =  "This is more like a real application, with lots of users and roles/permissions. " +
                                       "It also has admin of users and their roles plus an example hierarchical multi-tenant retail system.";
    }
}