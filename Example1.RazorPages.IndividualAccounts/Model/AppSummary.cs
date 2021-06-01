// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example1.RazorPages.IndividualAccounts.Model
{
    public class AppSummary
    {
        public string Application { get; } = "ASP.NET Core, Razor Pages";
        public string AuthenticationType { get; } = "Cookies";
        public string Users { get; } = "ASP.NET Core's individual accounts";
        public string Roles { get; } = "Handled by AuthPermissions";
        public string DataKey { get; } = "ASP.NET Core, Razor Pages";
        public string Databases { get; } = "ASP.NET Core, Razor Pages";
        public string Note { get; } =  "Also has ASP.NET Core's individual account roles, to show how AuThPermissions is uses role differently";
    }
}