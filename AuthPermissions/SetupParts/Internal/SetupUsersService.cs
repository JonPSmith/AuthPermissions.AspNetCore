// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using StatusGeneric;

[assembly: InternalsVisibleTo("Test")]
namespace AuthPermissions.SetupParts.Internal
{
    internal class SetupUsersService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly IFindUserIdService _findUserIdService;

        public SetupUsersService(AuthPermissionsDbContext context, IFindUserIdService findUserIdService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _findUserIdService = findUserIdService;
        }

        public async Task<IStatusGeneric> AddUsersRolesToDatabaseIfEmptyAsync(List<DefineUserWithRolesTenant> userDefinitions)
        {
            var status = new StatusGenericHandler();

            if (userDefinitions == null || !userDefinitions.Any())
                return status;

            if (_context.UserToRoles.Any())
            {
                status.Message =
                    "There were already Users in the auth database, so didn't add the user information";
                return status;
            }

            for (int i = 0; i < userDefinitions.Count; i++)
            {
                status.CombineStatuses(await CreateUserTenantAndAddToDbAsync(userDefinitions[i], i));
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
                (name, startOfName) => 
                {
                    var roleToPermission = _context.RoleToPermissions.SingleOrDefault(x => x.RoleName == name);
                    if (roleToPermission == null)
                        status.AddError(userDefine.RoleNamesCommaDelimited.FormErrorString(index, startOfName,
                            $"The role {name} wasn't found in the auth database."));
                    else
                        rolesToPermissions.Add(roleToPermission);
                });

            if (!rolesToPermissions.Any())
                status.AddError(userDefine.RoleNamesCommaDelimited.FormErrorString(index-1, -1,
                    $"The user {userDefine.UserName} didn't have any roles."));

            if (status.HasErrors)
                return status;

            var userId = userDefine.UserId;
            if (userId == null && _findUserIdService != null)
                userId = await _findUserIdService.FindUserIdAsync(userDefine.UniqueUserName);
            if (userId == null)
                return status.AddError(userDefine.UniqueUserName.FormErrorString(index - 1, -1,
                    $"The user {userDefine.UserName} didn't have a userId and the {nameof(IFindUserIdService)}" +
                    (_findUserIdService == null ? " wasn't available." : " couldn't find it either.")));

            rolesToPermissions.ForEach(roleToPermission =>
            {
                var userToRole = new UserToRole(userId, userDefine.UserName, roleToPermission);
                _context.Add(userToRole);
            });

            return status;
        }
    }
}