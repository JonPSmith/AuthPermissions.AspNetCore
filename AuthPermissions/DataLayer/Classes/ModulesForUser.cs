// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This holds what modules a user or tenant has
    /// </summary>
    public class ModulesForUser : TenantBase
    {
        /// <summary>
        /// This links modules to a user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="allowedPaidForModules"></param>
        /// <param name="tenantId"></param>
        public ModulesForUser(string userId, Enum allowedPaidForModules, string tenantId = null)
            : base(tenantId)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            AllowedPaidForModules = allowedPaidForModules;
        }

        [MaxLength(ExtraAuthConstants.UserIdSize)]
        public string UserId { get; private set; }

        public Enum AllowedPaidForModules { get; private set; }
    }
}