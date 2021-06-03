// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    public class UserToTenant : TenantBase
    {
        private UserToTenant() {} //Needed by EF Core

        public UserToTenant(string userId, Tenant tenant, string userName)
        {
            UserId = userId;
            UserName = userName;
            Tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        }

        //That has to be defined by EF Core's fluent API
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        //Contains a name to help work out who the user is
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }

        //----------------------------------------------------------
        //relationships

        /// <summary>
        /// Link to Tenant containing the DataKey
        /// </summary>
        public Tenant Tenant { get; private set; }

        public override string ToString()
        {
            var tenantRef = Tenant == null ? TenantId.ToString() : Tenant.TenantName;
            return $"User {UserName} is linked to tenant {tenantRef}";
        }
    }
}