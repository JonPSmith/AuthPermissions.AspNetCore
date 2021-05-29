// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupParts.Internal;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.SetupParts
{
    public class SetupUsersService
    {
        private readonly AuthPermissionsDbContext _context;

        public SetupUsersService(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        public async Task<IStatusGeneric> AddUsersToDatabaseIfEmpty(List<DefineUserWithRolesTenant> userDefinitions)
        {
            var status = new StatusGenericHandler();

            if (_context.UserToRoles.Any())
            {
                status.Message =
                    "There were already Users in the auth database, so didn't add the user information";
                return status;
            }

            for (int i = 0; i < userDefinitions.Count; i++)
            {
                status.CombineStatuses( await CreateUserTenantAndAddToDbAsync(userDefinitions[i], i));
            }

            status.Message = $"Added {userDefinitions.Count} new users with associated data to the auth database";
            return status;
        }

        //------------------------------------------
        //private methods

        private async Task<IStatusGeneric> CreateUserTenantAndAddToDbAsync(DefineUserWithRolesTenant userDefine, int index)
        {
            var status = new StatusGenericHandler();

            var rolesToPermissions = new List<RoleToPermissions>();
            userDefine.RoleNamesCommaDelimited.DecodeCheckCommaDelimitedString(0, 
                async (name, startOfName) => 
                {
                    var roleToPermission = await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == name);
                    if (roleToPermission == null)
                        status.AddError(userDefine.RoleNamesCommaDelimited.FormErrorString(index, startOfName,
                            $"The role {name} wasn't found in the auth database."));
                    else
                        rolesToPermissions.Add(roleToPermission);
                });

            if (!rolesToPermissions.Any())
                status.AddError(userDefine.RoleNamesCommaDelimited.FormErrorString(index-1, -1,
                    $"The user {userDefine.UserName ?? userDefine.UserId} didn't have any roles."));

            if (status.HasErrors)
                return status;

            rolesToPermissions.ForEach(roleToPermission =>
            {
                var userToRole = new UserToRole(userDefine.UserId, userDefine.UserName, roleToPermission, userDefine.TenantId);
                _context.Add(userToRole);
            });

            return status;
        }
    }
}