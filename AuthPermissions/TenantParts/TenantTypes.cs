// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.TenantParts
{
    /// <summary>
    /// This defines the types of tenant the AuthPermissions can handle
    /// </summary>
    public enum TenantTypes
    {
        /// <summary>
        /// Usage of tenants are turned off
        /// </summary>
        NotUsingTenants,
        /// <summary>
        /// Multi-tenant with one level only, e.g. a company has different departments: sales, finance, HR etc.
        /// A User can only be in one of these levels
        /// </summary>
        SingleTenant,
        /// <summary>
        /// Multi-tenant many levels, e.g. Holding company -> USA branch -> East Coast -> New York
        /// A User at the USA branch has read/write access to the USA branch data, read-only access to the East Coast and all its subsidiaries 
        /// </summary>
        HierarchicalTenant
    }
}