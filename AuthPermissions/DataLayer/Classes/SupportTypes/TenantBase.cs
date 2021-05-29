// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace AuthPermissions.DataLayer.Classes.SupportTypes
{
    public abstract class TenantBase
    {
        public TenantBase(string tenantId)
        {
            TenantId = tenantId ?? AuthDbConstants.DefaultTenantIdValue;
        }

        //A composite key can't be null in EF Core 
        //see https://github.com/dotnet/efcore/issues/22196
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantIdSize)]
        public string TenantId { get; private set; }
    }
}