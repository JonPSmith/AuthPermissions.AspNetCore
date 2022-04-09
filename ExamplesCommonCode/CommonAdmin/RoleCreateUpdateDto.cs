// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.SetupCode;

namespace ExamplesCommonCode.CommonAdmin
{
    public class RoleCreateUpdateDto
    {
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; set; }

        public string Description { get; set; }

        public RoleTypes RoleType { get; set; }

        public List<PermissionInfoWithSelectDto> PermissionsWithSelect { get; set; }

        public IEnumerable<string> GetSelectedPermissionNames()
        {
            return PermissionsWithSelect
                .Where(x => x.Selected)
                .Select(x => x.PermissionName);
        }

        public static RoleCreateUpdateDto SetupForCreateUpdate(string roleName, string description, 
            List<string> rolePermissions, List<PermissionDisplay> allPermissionNames,
            RoleTypes roleType = RoleTypes.Normal)
        {
            rolePermissions ??= new List<string>();

            return new RoleCreateUpdateDto
            {
                RoleName = roleName,
                Description = description,
                RoleType = roleType,
                PermissionsWithSelect = allPermissionNames
                    .Select(x => new PermissionInfoWithSelectDto
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