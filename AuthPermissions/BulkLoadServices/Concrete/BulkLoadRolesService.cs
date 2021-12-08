// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AuthPermissions.BulkLoadServices.Concrete.Internal;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode.Internal;
using AuthPermissions.SetupCode;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices.Concrete
{
    /// <summary>
    /// This bulk loads Roles with their permissions from a string with contains a series of lines
    /// </summary>
    public class BulkLoadRolesService : IBulkLoadRolesService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly Type _enumPermissionType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        public BulkLoadRolesService(AuthPermissionsDbContext context, AuthPermissionsOptions options)
        {
            _context = context;
            _enumPermissionType = options.InternalData.EnumPermissionsType;
        }

        /// <summary>
        /// This allows you to add Roles with their permissions via the <see cref="BulkLoadRolesDto"/> class
        /// </summary>
        /// <param name="roleSetupData">A list of definitions containing the information for each Role</param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> AddRolesToDatabaseAsync(List<BulkLoadRolesDto> roleSetupData)
        {
            var status = new StatusGenericHandler();

            if (roleSetupData == null || !roleSetupData.Any())
                return status;

            var enumNames = Enum.GetNames(_enumPermissionType);

            foreach (var roleDefinition in roleSetupData)
            {
                var permissionNames = roleDefinition.PermissionsCommaDelimited
                    .Split(',').Select(x => x.Trim()).ToList();
                var validPermissions = true;
                foreach (var permissionName in permissionNames)
                {
                    if (!enumNames.Contains(permissionName))
                    {
                        status.AddError($"Bulk load of Role '{roleDefinition.RoleName}' has a permission called " +
                                        $"{permissionName} which wasn't found in the Enum {_enumPermissionType.Name}");
                        validPermissions = false;
                    }
                }

                if (validPermissions)
                {
                    var role = new RoleToPermissions(roleDefinition.RoleName, roleDefinition.Description, 
                        _enumPermissionType.PackPermissionsNames(permissionNames.Distinct()), roleDefinition.RoleType);
                    _context.Add(role);
                }
            }

            if (status.IsValid)
                status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = $"Added {roleSetupData.Count} new RoleToPermissions to the auth database"; //If there is an error this message is removed
            return status;
        }
    }
}