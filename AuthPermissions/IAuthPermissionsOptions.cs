// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.SetupCode;

namespace AuthPermissions
{
    public interface IAuthPermissionsOptions
    {
        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// </summary>
        TenantTypes TenantType { get; set; }

        /// <summary>
        /// This holds data that is set up during the 
        /// </summary>
        SetupInternalData InternalData { get; }
    }
}