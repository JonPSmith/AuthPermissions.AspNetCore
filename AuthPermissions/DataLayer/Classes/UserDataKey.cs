// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    public class UserDataKey : TenantBase
    {
        public UserDataKey(string userId, string dataKey, Guid tenantId = default)
            : base(tenantId)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            DataKey = dataKey ?? throw new ArgumentNullException(nameof(dataKey));
        }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.DataKeySize)]
        public string DataKey { get; private set; }
    }
}