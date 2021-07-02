// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;

namespace AuthPermissions
{
    public interface IAuthPermissionsOptions
    {
        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// Defaults no using tenants
        /// </summary>
        TenantTypes TenantType { get; set; }

        /// <summary>
        /// This contains your decision on whether the AuthPermission's database is
        /// created/migrated on the startup of your ASP.NET Core application
        /// Values are:
        /// null: You haven't filled it in so you get an exception explaining your options
        /// false: You will have to create/migrate the <see cref="AuthPermissionsDbContext"/> database before you run your application
        /// true: AuthP will create/migrate the <see cref="AuthPermissionsDbContext"/> database on startup.
        ///       WARNING: this will FAIL in various situations, such as multiple instances of the app trying to all trying to migrate the same database.
        /// </summary>
        bool? MigrateAuthPermissionsDbOnStartup { get; set; }

        /// <summary>
        /// This is where you configure the JwtToken
        /// </summary>
        AuthJwtConfiguration ConfigureAuthJwtToken { get; set; }

        /// <summary>
        /// This holds data that is set up during the 
        /// </summary>
        SetupInternalData InternalData { get; }
    }
}