// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example5.MvcWebApp.AzureAdB2C.Models
{
    public class AppSummaryPlus
    {
        public string Application { get; } = "ASP.NET Core MVC";
        public string AuthorizationProvider { get; } = "Azure Active Directory (Azure AD)";
        public string CookieOrToken { get; } = "Cookie";
        public string[] Databases { get; } = new []
        {
            "AuthPermissions' database only"
        };
        public string Note { get; } = "This example assumes the Azure AD is linked to a company, i.e. users are created outside the application." ;

        public string WhatTypeOfAuthUser { get; set; }
    }
}