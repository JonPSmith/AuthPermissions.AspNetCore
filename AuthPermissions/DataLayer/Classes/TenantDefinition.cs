// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    public class TenantDefinition :TenantBase
    {
        public TenantDefinition(Guid tenantId, string tenantName)
            : base(tenantId)
        {

        }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantNameSize)]
        public string TenantName { get; private set; }
    }
}