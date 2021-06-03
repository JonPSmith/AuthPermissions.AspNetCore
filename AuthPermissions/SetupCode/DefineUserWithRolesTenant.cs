// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.SetupCode
{
    public class DefineUserWithRolesTenant
    {
        /// <summary>
        /// This defines a user in your application
        /// </summary>
        /// <param name="userName">name to help the admin team to work out who the user is</param>
        /// <param name="roleNamesCommaDelimited">A string containing a comma delimited set of auth roles that the user</param>
        /// <param name="userId"></param>
        /// <param name="uniqueUserName">A string that is unique for each user, e.g. email. If not provided then uses the userName</param>
        /// <param name="tenantNameForDataKey">Optional: The unique name of your multi-tenant that this user is linked to</param>
        public DefineUserWithRolesTenant(string userName, string roleNamesCommaDelimited,
            string userId = null,
            string uniqueUserName = null, string tenantNameForDataKey = null)
        {
            UserId = userId; //Can be null
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            RoleNamesCommaDelimited = roleNamesCommaDelimited ??
                                      throw new ArgumentNullException(nameof(roleNamesCommaDelimited));
            UniqueUserName = uniqueUserName ?? UserName;
            TenantNameForDataKey = tenantNameForDataKey;
        }

        /// <summary>
        /// This is what AuthPermissions needs to setup the <see cref="UserToRole"/>
        /// You can add the userId directly or provide a FindUserId function to set value
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Contains a name to help the admin team to work out who the user is
        /// </summary>
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }

        /// <summary>
        /// This contains a string that is unique for each user, e.g. email
        /// </summary>
        public string UniqueUserName { get; private set; }

        /// <summary>
        /// This contains the Tenant name. Used to provide the user with a multi-tenant data key
        /// </summary>
        public string TenantNameForDataKey { get; private set; }

        /// <summary>
        /// List of role names in a comma delimited list
        /// </summary>
        public string RoleNamesCommaDelimited { get; private set; }
    }
}