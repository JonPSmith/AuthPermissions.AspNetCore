// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;

namespace AuthPermissions
{
    public class AuthPermissionsOptions
    {

        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// Defaults no using tenants
        /// </summary>
        public TenantTypes TenantType { get; set; }

        /// <summary>
        /// This is where you configure the JwtToken
        /// </summary>
        public AuthPJwtConfiguration ConfigureAuthPJwtToken { get; set; }

        //-------------------------------------------------
        //internal set properties/handles

        /// <summary>
        /// This holds data that is set up during the 
        /// </summary>
        public SetupInternalData InternalData { get; } = new SetupInternalData();

    }
}