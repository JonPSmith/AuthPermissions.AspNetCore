// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace ExamplesCommonCode.CommonAdmin
{
    public class AuthUserDisplay
    {
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.EmailSize)]
        public string Email { get; private set; }
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }
        public string[] RoleNames { get; private set; }
        public bool HasTenant => TenantName != null;
        public string TenantName { get; private set; }

        public static IQueryable<AuthUserDisplay> TurnIntoDisplayFormat(IQueryable<AuthUser> inQuery)
        {
            return inQuery.Select(x => new AuthUserDisplay
            {
                UserName = x.UserName,
                Email = x.Email,
                UserId = x.UserId,
                RoleNames = x.UserRoles.Select(y => y.RoleName).ToArray(),
                TenantName = x.UserTenant.TenantFullName
            });
        }

        public static AuthUserDisplay DisplayUserInfo(AuthUser authUser)
        {
            var result = new AuthUserDisplay
            {
                UserName = authUser.UserName,
                Email = authUser.Email,
                UserId = authUser.UserId,
                TenantName = authUser.UserTenant?.TenantFullName
            };
            if (authUser.UserRoles != null)
                result.RoleNames = authUser.UserRoles.Select(y => y.RoleName).ToArray();

            return result;
        }
    }
}