// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions.DataLayer.Classes;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class AuthUserDisplay
    {
        public string UserName { get;  set; }
        public string Email { get; set; }
        public string UserId { get; set; }
        public int NumRoles { get; set; }
        public bool HasTenant { get; set; }

        public static IQueryable<AuthUserDisplay> SelectQuery(IQueryable<AuthUser> inQuery)
        {
            return inQuery.Select(x => new AuthUserDisplay
            {
                UserName = x.UserName,
                Email = x.Email,
                UserId = x.UserId,
                NumRoles = x.UserRoles.Count(),
                HasTenant = x.TenantId != null
            });
        }
    }
}