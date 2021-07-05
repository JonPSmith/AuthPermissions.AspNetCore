// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;


namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This is used for multi-tenant systems
    /// </summary>
    public class Tenant : INameToShowOnException
    {
        private HashSet<Tenant> _children;
        

        private Tenant() { } //Needed by EF Core

        /// <summary>
        /// This defines a tenant in a single tenant multi-tenant system.
        /// </summary>
        /// <param name="tenantName"></param>
        public Tenant(string tenantName)
        {
            TenantName = tenantName ?? throw new ArgumentNullException(nameof(tenantName));
        }

        /// <summary>
        /// This creates a tenant in a hierarchical multi-tenant system with a parent/child relationships
        /// You MUST have parent loaded and has been written to the database
        /// </summary>
        /// <param name="tenantName">This must be the full tenant name, including the parent name</param>
        /// <param name="parent"></param>
        public Tenant(string tenantName, Tenant parent)
        {
            TenantName = tenantName ?? throw new ArgumentNullException(nameof(tenantName));
            //We check that the higher layer has a primary key
            if (parent?.TenantId == (int)default)
                throw new AuthPermissionsException(
                    "The parent in the hierarchical setup doesn't have a valid primary key");

            ParentDataKey = parent?.GetTenantDataKey();
            Parent = parent;
            IsHierarchical = true;
        }

        /// <summary>
        /// Tenant primary key
        /// </summary>
        public int TenantId { get; private set; }

        /// <summary>
        /// This the combines primary key of all parents (can be null)
        /// </summary>
        [MaxLength(AuthDbConstants.TenantDataKeySize)]
        public string ParentDataKey { get; private set; }

        /// <summary>
        /// This is the name defined for this tenant. This is unique 
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantNameSize)]
        public string TenantName { get; private set; }

        /// <summary>
        /// This is true if the tenant is an hierarchical 
        /// </summary>
        public bool IsHierarchical { get; private set; }

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

        /// <summary>
        /// Easy way to see the tenant and its key
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{TenantName}: Key = {GetTenantDataKey()}";
        }

        //--------------------------------------------------
        // Exception Error name

        /// <summary>
        /// Used when there is an exception
        /// </summary>
        public string NameToUseForError => TenantName;

        //----------------------------------------------------
        //access methods

        /// <summary>
        /// This calculates the data key for this tenant.
        /// If it is a single layer multi-tenant it will by the TenantId as a string
        /// If it is a hierarchical multi-tenant it will contains a concatenation of the tenantsId in the parents as well
        /// </summary>
        public string GetTenantDataKey() => ParentDataKey + $".{TenantId}";

        /// <summary>
        /// This will provide a single tenant name.
        /// If its an hierarchical tenant, then it will be the last name in the hierarchy
        /// </summary>
        /// <returns></returns>
        public string GetTenantLastName() => ExtractEndLevelTenantName(this);

        /// <summary>
        /// This is the official way to combine the parent name and the individual tenant name
        /// </summary>
        /// <param name="thisTenantName">name for this specific tenant level</param>
        /// <param name="fullParentName"></param>
        /// <returns></returns>
        public static string CombineParentNameWithTenantName(string thisTenantName, string fullParentName)
        {
            if (thisTenantName == null) throw new ArgumentNullException(nameof(thisTenantName));
            return fullParentName == null ? thisTenantName : $"{fullParentName} | {thisTenantName}";
        }

        /// <summary>
        /// This updates the tenant name 
        /// </summary>
        /// <param name="newNameAtThisLevel"></param>
        public void UpdateTenantName(string newNameAtThisLevel)
        {
            if (!IsHierarchical)
            {
                TenantName = newNameAtThisLevel;
                return;
            }

            //Its hierarchical, so need to change the names of all its children
            if (Children == null)
                throw new AuthPermissionsException("The children must be loaded to rename a hierarchical tenant");
            if (newNameAtThisLevel.Contains('|'))
                throw new AuthPermissionsBadDataException("The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order", 
                    nameof(newNameAtThisLevel));

            TenantName = CombineParentNameWithTenantName(newNameAtThisLevel, Parent?.TenantName);

            RecursivelyChangeChildNames(this, Children, (parent, child) =>
            {
                var thisLevelTenantName = ExtractEndLevelTenantName(child);
                child.TenantName = CombineParentNameWithTenantName(thisLevelTenantName, parent.TenantName);
            });

        }

        /// <summary>
        /// This moves the current tenant to a another tenant
        /// </summary>
        /// <param name="newParentTenant"></param>
        public void MoveTenantToNewParent(Tenant newParentTenant)
        {
            if (!IsHierarchical)
                throw new AuthPermissionsException("You can only move a hierarchical tenant to a new parent");
            if (Children == null)
                throw new AuthPermissionsException("The children must be loaded to move a hierarchical tenant");

            TenantName = CombineParentNameWithTenantName(ExtractEndLevelTenantName(this), newParentTenant?.TenantName);
            ParentDataKey = newParentTenant?.GetTenantDataKey();

            RecursivelyChangeChildNames(this, Children, (parent, child) =>
            {
                var thisLevelTenantName = ExtractEndLevelTenantName(child);
                child.TenantName = CombineParentNameWithTenantName(thisLevelTenantName, parent.TenantName);
                child.ParentDataKey = parent?.GetTenantDataKey();
            });
        }

        //-------------------------------------------------------
        // private methods

        /// <summary>
        /// This will recursively move through the children of a parent and call the action applies a change to each child
        /// </summary>
        /// <param name="parent">The parent of the children (can be null)</param>
        /// <param name="children"></param>
        /// <param name="updateTenant">This action takes the parent and child</param>
        private static void RecursivelyChangeChildNames(Tenant parent, IEnumerable<Tenant> children, Action<Tenant, Tenant> updateTenant)
        {
            foreach (var child in children)
            {
                updateTenant(parent, child);
                if (child.Children.Any())
                    RecursivelyChangeChildNames(child, child.Children, updateTenant);
            }
        }

        private static string ExtractEndLevelTenantName(Tenant tenant)
        {
            var lastIndex = tenant.TenantName.LastIndexOf('|');
            var thisLevelTenantName = lastIndex < 0 ? tenant.TenantName : tenant.TenantName.Substring(lastIndex+1).Trim();
            return thisLevelTenantName;
        }

    }
}