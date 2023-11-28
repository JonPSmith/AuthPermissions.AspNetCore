// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AspNetCore.OpenIdCode
{
    /// <summary>
    /// This contains the applications settings to access an Azure AD 
    /// </summary>
    public class AzureAdOptions
    {
        /// <summary>
        /// Name of the section in the appsettinsg file holding this data
        /// </summary>
        public const string SectionName = "AzureAd";

        public string Instance { get; set; }
        public string Domain { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string CallbackPath { get; set; }
        public string ClientSecret { get; set; }

        /// <summary>
        /// This is a AuthP setting and contains a comma delimited string that defines how the AzureAD
        /// AzureAD manager will manage the Azure AD user
        /// 1. "Find" means try to find a existing Azure AD user via its email
        /// 2. "Create" means it will create a new Azure AD
        /// </summary>
        public string AzureAdApproaches { get; set; } = "Find";
    }
}