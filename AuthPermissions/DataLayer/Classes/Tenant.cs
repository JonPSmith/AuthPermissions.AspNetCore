// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;


namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This is used for multi-tenant systems
    /// NOTE: two types of names are defined in the Tenant. For single-level tenants the two names are the same
    /// 1. FullTenantName - for hierarchical tenants this is the combined tenant names (separated by |) of the parents and its tenant name
    /// 2. TenantName - the last name in FullTenantName
    /// The FullTenantName is saved to the database, but the TenantName is derived from the FullTenantName
    /// </summary>
    public class Tenant : INameToShowOnException, ITenantPartsToExport
    {
#pragma warning disable 649
        // ReSharper disable once CollectionNeverUpdated.Local
        private HashSet<Tenant> _children; //filled in by EF Core
#pragma warning restore 649

        private Tenant() { } //Needed by EF Core

        /// <summary>
        /// This defines a tenant in a single tenant multi-tenant system.
        /// </summary>
        /// <param name="fullTenantName"></param>
        public Tenant(string fullTenantName)
        {
            TenantFullName = fullTenantName?.Trim() ?? throw new ArgumentNullException(nameof(fullTenantName));
        }

        /// <summary>
        /// This creates a tenant in a hierarchical multi-tenant system with a parent/child relationships
        /// You MUST have parent loaded and has been written to the database
        /// </summary>
        /// <param name="fullTenantName">This must be the full tenant name, including the parent name</param>
        /// <param name="parent"></param>
        public Tenant(string fullTenantName, Tenant parent)
        {
            TenantFullName = fullTenantName?.Trim() ?? throw new ArgumentNullException(nameof(fullTenantName));
            
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
        /// This is the name defined for this tenant, and must be unique
        /// In hierarchical tenants this is the combined tenant names (separated by |) of the parents and its tenant name
        /// NOTE: The TenantName is the last name in the list of names
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]
        public string TenantFullName { get; private set; }

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
            return $"{TenantFullName}: Key = {GetTenantDataKey()}";
        }

        //--------------------------------------------------
        // Exception Error name

        /// <summary>
        /// Used when there is an exception
        /// </summary>
        public string NameToUseForError => TenantFullName;

        //----------------------------------------------------
        //access methods

        /// <summary>
        /// This calculates the data key for this tenant.
        /// If it is a single layer multi-tenant it will by the TenantId as a string
        /// If it is a hierarchical multi-tenant it will contains a concatenation of the tenantsId in the parents as well
        /// </summary>
        public string GetTenantDataKey()
        {
            if (TenantId == default)
                throw new AuthPermissionsException(
                    "The Tenant DataKey is only correct if the tenant primary key is set");

            return ParentDataKey + $".{TenantId}";
        }

        /// <summary>
        /// This will provide a single tenant name.
        /// If its an hierarchical tenant, then it will be the last name in the hierarchy
        /// </summary>
        /// <returns></returns>
        public string GetTenantName() => ExtractEndLeftTenantName(TenantFullName);

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
            if (newNameAtThisLevel == null) throw new ArgumentNullException(nameof(newNameAtThisLevel));
            if (!IsHierarchical)
            {
                TenantFullName = newNameAtThisLevel.Trim();
                return;
            }

            //Its hierarchical, so need to change the names of all its children
            if (Children == null)
                throw new AuthPermissionsException("The children must be loaded to rename a hierarchical tenant");
            if (newNameAtThisLevel.Contains('|'))
                throw new AuthPermissionsBadDataException("The tenant name must not contain the character '|' because that character is used to separate the names in the hierarchical order", 
                    nameof(newNameAtThisLevel));

            TenantFullName = CombineParentNameWithTenantName(newNameAtThisLevel.Trim(), Parent?.TenantFullName);

            RecursivelyChangeChildNames(this, Children, (parent, child) =>
            {
                var thisLevelTenantName = ExtractEndLeftTenantName(child.TenantFullName);
                child.TenantFullName = CombineParentNameWithTenantName(thisLevelTenantName, parent.TenantFullName);
            });

        }

        /// <summary>
        /// This moves the current tenant to a another tenant
        /// </summary>
        /// <param name="newParentTenant">Can be null if moving to top</param>
        /// <param name="getOldNewData">This action is called at every tenant that is effected.
        /// This allows you to obtains the previous DataKey, the new DataKey and the fullname of every tenant that was moved</param>
        public void MoveTenantToNewParent(Tenant newParentTenant,
            Action<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> getOldNewData)
        {
            if (!IsHierarchical)
                throw new AuthPermissionsException("You can only move a hierarchical tenant to a new parent");
            if (Children == null)
                throw new AuthPermissionsException("The children must be loaded to move a hierarchical tenant");

            var oldDataKey = GetTenantDataKey();
            TenantFullName = CombineParentNameWithTenantName(ExtractEndLeftTenantName(this.TenantFullName), newParentTenant?.TenantFullName);
            Parent = newParentTenant;
            ParentDataKey = newParentTenant?.GetTenantDataKey();
            getOldNewData((oldDataKey, GetTenantDataKey(), TenantId, TenantFullName));

            RecursivelyChangeChildNames(this, Children, (parent, child) =>
            {
                var thisLevelTenantName = ExtractEndLeftTenantName(child.TenantFullName);
                child.TenantFullName = CombineParentNameWithTenantName(thisLevelTenantName, parent.TenantFullName);
                var previousDataKey = child.GetTenantDataKey();
                child.ParentDataKey = parent?.GetTenantDataKey();
                var newDataKey = child.GetTenantDataKey();
                getOldNewData?.Invoke((previousDataKey, newDataKey, child.TenantId, child.TenantFullName));
            });
        }

        /// <summary>
        /// This will return a single tenant name. If it's hierarchical it returns the final name
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <returns></returns>
        public static string ExtractEndLeftTenantName(string fullTenantName)
        {
            var lastIndex = fullTenantName.LastIndexOf('|');
            var thisLevelTenantName = lastIndex < 0 ? fullTenantName : fullTenantName.Substring(lastIndex + 1).Trim();
            return thisLevelTenantName;
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


    }
}