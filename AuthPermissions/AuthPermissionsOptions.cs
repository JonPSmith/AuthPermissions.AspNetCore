// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.SetupCode;

namespace AuthPermissions
{
    /// <summary>
    /// This contains the options set by the developer and data that is passed between setup extension methods
    /// </summary>
    public class AuthPermissionsOptions
    {

        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// Defaults no using tenants
        /// </summary>
        public TenantTypes TenantType { get; set; }

        /// <summary>
        /// This turns on the LinkToTenantData feature, e.g. an admin person can access the data in a specific tenant
        /// </summary>
        public LinkToTenantTypes LinkToTenantType { get; set; }

        /// <summary>
        /// You should set this property to your application's ConnectionString if you are using Tenants
        /// It's used with the <see cref="ITenantChangeService"/> to update, move, or delete the tenant data in your application
        /// </summary>
        public string AppConnectionString { get; set; }

        /// <summary>
        /// This will use the Net.RunMethodsSequentially library to safely update / seed a database 
        /// on applications that have mutiple instances using a global lock
        /// </summary>
        public bool UseLocksToUpdateGlobalResources { get; set; } = true;

        /// <summary>
        /// This is used by the Net.RunMethodsSequentially library to lock a folder
        /// If UseRunMethodsSequentially is true, then this propery must be filled 
        /// with a path to a directory in your running application 
        /// </summary>
        public string PathToFolderToLock { get; set; }

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