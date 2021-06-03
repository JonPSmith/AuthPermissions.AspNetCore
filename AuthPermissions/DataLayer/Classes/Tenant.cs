// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This is used for multi-tenant systems
    /// </summary>
    [Index(nameof(TenantName), IsUnique = true)]
    public class Tenant :TenantBase
    {
        private HashSet<Tenant> _children;
        private string _parentDataKey;

        /// <summary>
        /// This defines a tenant in a single tenant multi-tenant system.
        /// </summary>
        /// <param name="tenantName"></param>
        public Tenant(string tenantName)
        {
            TenantName = tenantName ?? throw new ArgumentNullException(nameof(tenantName));
        }

        private Tenant(string tenantName, string parentDataKey, Tenant parent)
        {
            TenantName = tenantName ?? throw new ArgumentNullException(nameof(tenantName));
            _parentDataKey = parentDataKey;
            Parent = parent;
        }


        /// <summary>
        /// This defines a tenant in a hierarchical multi-tenant system with a parent/child relationships
        /// You MUST a) have every parent layer loaded and b) all parents must have a valid primary key 
        /// </summary>
        public static Tenant SetupHierarchicalTenant(string tenantName, Tenant parent)
        {
            //We check that the higher layer has a primary key
            if (parent?.TenantId == (int)default)
                throw new AuthPermissionsException(
                    "The parent in the hierarchical setup doesn't have a valid primary key");

            return new Tenant(tenantName, parent?.TenantDataKey, parent);
        }

        /// <summary>
        /// Easy way to see the tenant and its key
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{TenantName}: Key = {TenantDataKey}";
        }

        /// <summary>
        /// This is the name defined for this tenant. This is unique 
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantNameSize)]
        public string TenantName { get; private set; }

        /// <summary>
        /// This contains the data key for this tenant.
        /// If it is a single layer multi-tenant it will by the TenantId as a string
        /// If it is a hierarchical multi-tenant it will contains a concatenation of the tenantsId
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantDataKeySize)]
        public string TenantDataKey => _parentDataKey + $".{TenantId}";

        //---------------------------------------------------------
        //relationships - only used for hierarchical multi-tenant system

        /// <summary>
        /// Foreign key to parent - can by null
        /// </summary>
        public int? ParentTenantId { get; private set; }

        /// <summary>
        /// The parent tenant (if it exists)
        /// </summary>
        [ForeignKey(nameof(ParentTenantId))]
        public Tenant Parent { get; private set; }

        /// <summary>
        /// The optional children
        /// </summary>
        public IReadOnlyCollection<Tenant> Children => _children?.ToList();

    }
}