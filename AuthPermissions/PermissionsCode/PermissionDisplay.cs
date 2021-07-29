// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AuthPermissions.PermissionsCode
{
    /// <summary>
    /// This class holds information on one enum in the permissions enum with the various attributes
    /// It also holds a static method for returning all the permissions for a specific enum type
    /// </summary>
    public class PermissionDisplay
    {
        private PermissionDisplay(string permissionName,
            string groupName, string name, string description)
        {
            PermissionName = permissionName;
            GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            ShortName = name  ?? "<none>";
            Description = description ?? "<none>";
        }

        /// <summary>
        /// GroupName, which groups permissions working in the same area
        /// </summary>
        public string GroupName { get; private set; }

        /// <summary>
        /// ShortName of the permission - often says what it does, e.g. Read
        /// </summary>
        public string ShortName { get; private set; }

        /// <summary>
        /// Long description of what action this permission allows 
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// The name of the permission
        /// </summary>
        public string PermissionName { get; private set; }

        /// <summary>
        /// Details of the permission names with the extra info provided by the 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{PermissionName}: Group: {GroupName}, ShortName: {ShortName}, Description: {Description}";
        }


        /// <summary>
        /// This returns all the enum permission names with the various display attribute data
        /// NOTE: It does not show enum names that
        /// a) don't have an <see cref="DisplayAttribute"/> on them. They are assumed to not 
        /// b) Which have a <see cref="ObsoleteAttribute"/> applied to that name
        /// </summary>
        /// <param name="enumType">type of the enum permissions</param>
        /// <param name="includeFilteredPermissions">if false then it won't show permissions where the AutoGenerateFilter is true</param>
        /// <returns>a list of PermissionDisplay classes containing the data</returns>
        public static List<PermissionDisplay> GetPermissionsToDisplay(Type enumType, bool includeFilteredPermissions = false) 
        {
            var result = new List<PermissionDisplay>();
            foreach (var permissionName in Enum.GetNames(enumType))
            {
                var member = enumType.GetMember(permissionName);
                //This allows you to obsolete a permission and it won't be shown as a possible option, but is still there so you won't reuse the number
                var obsoleteAttribute = member[0].GetCustomAttribute<ObsoleteAttribute>();
                if (obsoleteAttribute != null)
                    continue;

                //If there is no DisplayAttribute then the Enum is not used
                var displayAttribute = member[0].GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute == null)
                    continue;

                //remove permissions where AutoGenerateFilter is true
                if (!includeFilteredPermissions && displayAttribute.GetAutoGenerateFilter() == true)
                    continue;

                result.Add(new PermissionDisplay(permissionName, displayAttribute.GroupName, displayAttribute.Name, displayAttribute.Description));
            }

            return result;
        }
    }
}