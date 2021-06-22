// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This contains a 
    /// </summary>
    public class RolesContainingObsoletePermission
    {
        internal RolesContainingObsoletePermission(string obsoletePermissionName, List<string> roleNames)
        {
            ObsoletePermissionName = obsoletePermissionName;
            RoleNames = roleNames;
        }

        /// <summary>
        /// The name of the enum member that is obsolete 
        /// </summary>
        public string ObsoletePermissionName { get; }

        /// <summary>
        /// The name of all the Roles that link to this obsolete enum member 
        /// </summary>
        public List<string> RoleNames { get; }
    }
}