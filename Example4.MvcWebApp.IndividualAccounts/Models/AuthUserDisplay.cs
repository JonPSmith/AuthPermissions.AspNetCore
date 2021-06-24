// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class AuthUserDisplay
    {
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get;  set; }
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.EmailSize)]
        public string Email { get; set; }
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
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

        public static AuthUserDisplay DisplayUserInfo(AuthUser authUser)
        {
            return new AuthUserDisplay
            {
                UserName = authUser.UserName,
                Email = authUser.Email,
                UserId = authUser.UserId,
            };
        }
    }
}