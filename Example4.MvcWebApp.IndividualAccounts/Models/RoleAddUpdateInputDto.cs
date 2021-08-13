// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class RoleAddUpdateInputDto
    {
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; set; }

        public string Description { get; set; }

        public List<string> Permissions { get; set; }
    }
}