// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BulkLoadServices.Concrete.Internal;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode.Internal;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices.Concrete
{
    /// <summary>
    /// This bulk loads Roles with their permissions from a string with contains a series of lines
    /// </summary>
    public class BulkLoadRolesService : IBulkLoadRolesService
    {
        private readonly AuthPermissionsDbContext _context;

        public BulkLoadRolesService(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This allows you to add Roles with their permissions from a string with contains a series of lines
        /// (a line is ended with <see cref="Environment.NewLine"/>
        /// </summary>
        /// <param name="linesOfText">This contains the lines of text, each line defined a Role with Permissions. The format is
        /// RoleName |optional-description|: PermissionName, PermissionName, PermissionName... and so on
        /// For example:
        /// SalesManager |Can authorize and alter sales|: SalesRead, SalesAdd, SalesUpdate, SalesAuthorize
        /// </param>
        /// <param name="enumPermissionType"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddRolesToDatabaseAsync(string linesOfText, Type enumPermissionType)
        {
            IStatusGeneric status = new StatusGenericHandler();

            if (string.IsNullOrEmpty(linesOfText))
                return status;

            var lines = linesOfText.Split( Environment.NewLine);

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                status.CombineStatuses(DecodeLineAndAddToDb(lines[i], i, enumPermissionType));
            }

            if (status.IsValid)
                status = await _context.SaveChangesWithChecksAsync();

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
                charNum = indexLastBar + 2;
            }
            else
            {
                roleName = line.Substring(0, indexColon).Trim();
                charNum = indexColon + 2;
            }

            var validPermissionNames = line.DecodeCommaDelimitedNameWithCheck(charNum,
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

            var role = new RoleToPermissions(roleName, description, enumPermissionType.PackPermissionsNames(validPermissionNames.Distinct()));
            _context.Add(role);

            return status;
        }
    }
}