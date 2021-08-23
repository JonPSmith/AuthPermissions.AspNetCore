// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.PermissionsCode;

namespace ExamplesCommonCode.CommonAdmin
{
    public class PermissionInfoWithSelect
    {
        public string GroupName { get; set; }
        public string Description { get; set; }
        public string PermissionName { get; set; }
        public bool Selected { get; set; }
    }
}