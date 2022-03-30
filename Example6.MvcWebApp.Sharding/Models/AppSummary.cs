// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example6.MvcWebApp.Sharding.Models
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core MVC";
        public string AuthorizationProvider { get; } = "ASP.NET Core's individual users account";
        public string CookieOrToken { get; } = "Cookie";
        public string MultiTenant { get; } = "single level multi-tenant with sharding";
        public string[] Databases { get; } = new []
        {
            "One SQL Server database shared by:",
            "- ASP.NET Core Individual accounts database",
            "- AuthPermissions' database - tables have a schema of 'authp'",
            "- multi-tenant invoice database - tables have a schema of 'invoice'",
            "Other databases are available to show how sharding works"
        };

        public string Note { get; } = "Sharding with multiple databases";
    }
}