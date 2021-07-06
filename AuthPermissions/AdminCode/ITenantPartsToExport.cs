// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This interface is a useful for applications that want to refer to the Tenant in its code
    /// </summary>
    public interface ITenantPartsToExport
    {
        /// <summary>
        /// Tenant primary key of AuthP tenant
        /// </summary>
        public int TenantId { get; }

        /// <summary>
        /// This is the name defined for this tenant. This is unique 
        /// </summary>
        string TenantFullName { get; }

        /// <summary>
        /// This is true if the tenant is an hierarchical 
        /// </summary>
        bool IsHierarchical { get; }

        /// <summary>
        /// This calculates the data key for this tenant.
        /// If it is a single layer multi-tenant it will by the TenantId as a string
        /// If it is a hierarchical multi-tenant it will contains a concatenation of the tenantsId in the parents as well
        /// </summary>
        string GetTenantDataKey();

        /// <summary>
        /// This will provide a single tenant name.
        /// If its an hierarchical tenant, then it will be the last name in the hierarchy
        /// </summary>
        /// <returns></returns>
        string GetTenantEndLeafName();
    }
}