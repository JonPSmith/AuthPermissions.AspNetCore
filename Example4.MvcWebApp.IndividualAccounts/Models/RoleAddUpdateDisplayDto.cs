// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.PermissionsCode;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class RoleAddUpdateDisplayDto
    {
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; }

        public string Description { get; }

        public List<KeyValuePair<PermissionDisplay,bool>> AddPermissionsWithSelect { get; }

        public RoleAddUpdateDisplayDto(string roleName, string description, List<string> rolePermissions, List<PermissionDisplay> allPermissionNames)
        {
            RoleName = roleName;
            Description = description;

            rolePermissions ??= new List<string>();

            AddPermissionsWithSelect = allPermissionNames
                .Select(x => new KeyValuePair<PermissionDisplay, bool>(x, rolePermissions.Contains(x.PermissionName)))
                .ToList();
        }

    }
}