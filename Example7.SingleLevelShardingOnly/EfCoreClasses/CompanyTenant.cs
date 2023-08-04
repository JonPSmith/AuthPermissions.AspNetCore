// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example7.SingleLevelShardingOnly.EfCoreClasses
{
    public class CompanyTenant
    {
        public int CompanyTenantId { get; set; }

        /// <summary>
        /// This contains the fullname of the AuthP Tenant
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// This contains the Primary key from the AuthP's Tenant
        /// </summary>
        public int AuthPTenantId { get; set; }
    }
}