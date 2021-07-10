// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.CommonCode;
using Microsoft.EntityFrameworkCore;

namespace Example4.ShopCode.EfCoreClasses
{
    [Index(nameof(FullName), IsUnique = true)]
    public class RetailOutlet : IDataKeyFilter
    {
        private RetailOutlet() { } //Needed by EF Core

        public RetailOutlet(ITenantPartsToExport authPTenant)
        {
            if (authPTenant == null) throw new ArgumentNullException(nameof(authPTenant));

            FullName = authPTenant.TenantFullName;
            ShortName = authPTenant.GetTenantEndLeafName();
            DataKey = authPTenant.GetTenantDataKey();
            AuthPTenantId = authPTenant.TenantId;
        }

        public int RetailOutletId { get; private set; }

        /// <summary>
        /// This contains the fullname of the AuthP Tenant
        /// </summary>
        public string FullName { get; private set; }

        public string ShortName { get; private set; }

        /// <summary>
        /// This contains the datakey from the AuthP's Tenant
        /// </summary>
        public string DataKey { get; private set; }

        /// <summary>
        /// This is here in case a hierarchical AuthP Tenant has its position in the hierarchy changes.
        /// It this happens the DataKey and the FullName will change, so you need to update the RetailOutlet
        /// </summary>
        public int AuthPTenantId { get; private set; }
    }
}