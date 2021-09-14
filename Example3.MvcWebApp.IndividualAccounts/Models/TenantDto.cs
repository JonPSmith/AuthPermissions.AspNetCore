// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace Example3.MvcWebApp.IndividualAccounts.Models
{
    public class TenantDto
    {
        public int TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]
        public string TenantName { get; set; }

        public string DataKey { get; set; }


        public static IQueryable<TenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
        {
            return inQuery.Select(x => new TenantDto
            {
                TenantId = x.TenantId,
                TenantName = x.TenantFullName,
                DataKey = x.GetTenantDataKey()
            });
        }
    }
}