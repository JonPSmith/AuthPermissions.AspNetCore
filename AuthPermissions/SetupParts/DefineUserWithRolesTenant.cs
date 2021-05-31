// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.SetupParts
{
    public class DefineUserWithRolesTenant
    {
        public DefineUserWithRolesTenant(string userId, string userName, string roleNamesCommaDelimited, string tenantName = null)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            UserName = userName;
            RoleNamesCommaDelimited = roleNamesCommaDelimited ?? throw new ArgumentNullException(nameof(roleNamesCommaDelimited));
            TenantName = tenantName;
        }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        //Contains a name to help work out who the user is
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }

        public string TenantName { get; private set; }

        public string RoleNamesCommaDelimited { get; private set; }
    }
}