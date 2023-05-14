// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using RunMethodsSequentially;

namespace AuthPermissions.BaseCode.SetupCode
{
    /// <summary>
    /// This contains the data that is set during the set up the AuthPermissions
    /// </summary>
    public class SetupInternalData
    {
        /// <summary>
        /// holds the type of the Enum Permissions 
        /// </summary>
        public Type EnumPermissionsType { get; set; }

        /// <summary>
        /// This contains the type of database used for the AuthP database
        /// </summary>
        public AuthPDatabaseTypes AuthPDatabaseType { get; set; }

        /// <summary>
        /// This holds the Net.RunMethodsSequentially options 
        /// </summary>
        public RunSequentiallyOptions RunSequentiallyOptions { get; set; }

        /// <summary>
        /// this contains the type of authorization your application uses
        /// </summary>
        public AuthPAuthenticationTypes AuthPAuthenticationType { get; set; }

        /// <summary>
        /// This type defines the localization's recourse type which defines
        /// the recourse file group that holds the localized version of the AuthP messages.
        /// If null, then localization is not turned on.
        /// </summary>
        public Type AuthPResourceType { get; set; }

        /// <summary>
        /// When using localization you need to provide the supported cultures.
        /// </summary>
        public string[] SupportedCultures { get; set; }


        /// <summary>
        /// This holds the classes containing the definition of a RolesToPermission database class
        /// </summary>
        public List<BulkLoadRolesDto> RolesPermissionsSetupData { get; set; }

        /// <summary>
        /// This holds the classes that defines the tenants 
        /// Note for hierarchical tenants you add children tenants via an nested set of <see cref="BulkLoadTenantDto"/> classes
        /// </summary>
        public List<BulkLoadTenantDto> TenantSetupData { get; set; }

        /// <summary>
        /// This holds the definition for a user, with its various parts
        /// See the <see cref="BulkLoadUserWithRolesTenant"/> class for information you need to provide
        /// </summary>
        public List<BulkLoadUserWithRolesTenant> UserRolesSetupData { get; set; }
    }
}