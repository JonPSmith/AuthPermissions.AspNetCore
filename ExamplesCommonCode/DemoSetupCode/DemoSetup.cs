// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace ExamplesCommonCode.DemoSetupCode
{
    public class DemoSetup
    {
        public User[] Users { get; set; }
        
        //Format of this is "RoleName: PermissionName, PermissionName, etc..."
        public string[] RolesToPermissions { get; set; }
        public bool AddRolesToAspNetUser { get; set; }
    }

    public class User
    {
        public string Email { get; set; }
        public string RolesCommaDelimited { get; set; }
    }

}