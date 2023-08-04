// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example6.MvcWebApp.Sharding.Models
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core MVC";
        public string AuthorizationProvider { get; } = "ASP.NET Core's individual users account";
        public string CookieOrToken { get; } = "Cookie";
        public string MultiTenant { get; } = "single level multi-tenant using a hybrid sharding";
        public string[] Databases { get; } = new []
        {
            "A database used by AuthP, but can also used to hold tenants.",
            "There are four demo servers: Default, West, Center, and East",
        };

        public string? Note { get; } = null;
    }
}