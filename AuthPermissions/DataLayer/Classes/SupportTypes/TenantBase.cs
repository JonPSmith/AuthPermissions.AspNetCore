// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.DataLayer.Classes.SupportTypes
{
    public abstract class TenantBase
    {
        public TenantBase(string tenantId)
        {
            TenantId = tenantId;
        }

        [MaxLength(ExtraAuthConstants.TenantIdSize)]
        public string TenantId { get; private set; }
    }
}