// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example1.RazorPages.IndividualAccounts.Model
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core, Razor Pages";
        public string AuthorizationProvider { get; } = "ASP.NET Core's individual accounts";
        public string CookieOrToken { get; } = "Cookie";
        public string MultiTenant { get; } = "- not used -";
        public string[] Databases { get; } = new []
        {
            "Individual accounts: InMemory Database",
            "AuthPermissions: In-memory database (uses SQLite in-memory)"
        };
        public string Note { get; } =  "Shows basics of Roles and permissions, plus multi-language support.";
    }
}