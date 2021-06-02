// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode.Internal;
using StatusGeneric;

[assembly: InternalsVisibleTo("Test")]
namespace AuthPermissions.SetupParts.Internal
{
    internal class SetupRolesService
    {
        private readonly AuthPermissionsDbContext _context;

        public SetupRolesService(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        public IStatusGeneric AddRolesToDatabaseIfEmpty(string linesOfText, Type enumPermissionType)
        {
            var status = new StatusGenericHandler();

            if (string.IsNullOrEmpty(linesOfText))
                return status;

            if (_context.RoleToPermissions.Any())
            {
                status.Message =
                    "There were already RoleToPermissions in the auth database, so didn't add these roles";
                return status;
            }

            var lines = linesOfText.Split( Environment.NewLine);

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                status.CombineStatuses(DecodeLineAndAddToDb(lines[i], i, enumPermissionType));
            }

            if (status.IsValid)
                _context.SaveChanges();

            status.Message = $"Added {lines.Length} new RoleToPermissions to the auth database"; //If there is an error this message is removed
            return status;
        }

        //----------------------------------------
        //private methods

        private IStatusGeneric DecodeLineAndAddToDb(string line, int lineNum, Type enumPermissionType)
        {
            var status = new StatusGenericHandler();
            var indexColon = line.IndexOf(':');
            if (indexColon < 1)
                return status.AddError(line.FormErrorString(lineNum, -1,
                    "Could not find ':' that should be after the role name"));

            var indexFirstBar = line.IndexOf('|');
            string roleName;
            string description = null;
            int charNum;
            if (indexFirstBar > 0 && indexFirstBar < indexColon)
            {
                //we have a description
                var indexLastBar = line.LastIndexOf('|');
                if (indexLastBar == indexFirstBar)
                    return status.AddError(line.FormErrorString(lineNum, indexFirstBar+1,
                        $"There should be a '|' at the beginning and end of the description."));

                description = line.Substring(indexFirstBar+1, indexLastBar - (indexFirstBar + 1));

                roleName = line.Substring(0, indexFirstBar).Trim();
                charNum = indexLastBar + 1;
            }
            else
            {
                roleName = line.Substring(0, indexColon).Trim();
                charNum = indexColon + 2;
            }

            var validPermissionNames = line.DecodeCodeNameWithTrimming(charNum,
                (name, startOfName) =>
                {
                    if (!enumPermissionType.PermissionsNameIsValid(name))
                        status.AddError(line.FormErrorString(lineNum, startOfName,
                            $"The permission name {name} wasn't found in the Enum {enumPermissionType.Name}"));
                });

            if (!validPermissionNames.Any())
                status.AddError(line.FormErrorString(lineNum, line.Length-1,
                    $"The role {roleName} had no permissions."));

            if (status.HasErrors)
                return status;

            var roleStatus = RoleToPermissions.CreateRoleWithPermissions(roleName, description,
                enumPermissionType.PackPermissionsNames(validPermissionNames.Distinct()), _context);

            if (status.HasErrors)
                return status;

            _context.Add(roleStatus.Result);

            return status;
        }
    }
}