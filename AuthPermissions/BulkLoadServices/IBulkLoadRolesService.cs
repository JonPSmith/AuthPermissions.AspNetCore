// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices
{

    /// <summary>
    /// Bulk load many Roles with their permissions
    /// </summary>
    public interface IBulkLoadRolesService
    {
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
        Task<IStatusGeneric> AddRolesToDatabaseAsync(string linesOfText, Type enumPermissionType);
    }
}