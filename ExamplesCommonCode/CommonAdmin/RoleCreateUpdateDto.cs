// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.PermissionsCode;

namespace ExamplesCommonCode.CommonAdmin
{
    public class RoleCreateUpdateDto
    {
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; set; }

        public string Description { get; set; }

        public List<PermissionInfoWithSelect> PermissionsWithSelect { get; set; }

        public IEnumerable<string> GetSelectedPermissionNames()
        {
            return PermissionsWithSelect
                .Where(x => x.Selected)
                .Select(x => x.PermissionName);
        }

        public static RoleCreateUpdateDto SetupForCreateUpdate(string roleName, string description, List<string> rolePermissions, List<PermissionDisplay> allPermissionNames)
        {
            rolePermissions ??= new List<string>();

            return new RoleCreateUpdateDto
            {
                RoleName = roleName,
                Description = description,
                PermissionsWithSelect = allPermissionNames
                    .Select(x => new PermissionInfoWithSelect
                    {
                        GroupName = x.GroupName,
                        Description = x.Description,
                        PermissionName = x.PermissionName,
                        Selected = rolePermissions.Contains(x.PermissionName)
                    }).ToList()
            };
        }
    }
}