// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode.Internal;
using AuthPermissions.SetupParts.Internal;
using StatusGeneric;

namespace AuthPermissions.SetupParts
{
    public class SetupRolesService
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

            //https://stackoverflow.com/questions/45758587/how-can-i-turn-a-multi-line-string-into-an-array-where-each-element-is-a-line-of
            var lines = linesOfText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                status.CombineStatuses(DecodeLineAndAddToDb(lines[i], i, enumPermissionType));
            }

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

            var validPermissionNames = line.DecodeCheckCommaDelimitedString(charNum,
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