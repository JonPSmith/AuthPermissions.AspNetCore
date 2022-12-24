// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace AuthPermissions.BaseCode.DataLayer.Classes
{
    /// <summary>
    /// This is used for multi-tenant systems
    /// NOTE: two types of names are defined in the Tenant. For single-level tenants the two names are the same
    /// 1. FullTenantName - for hierarchical tenants this is the combined tenant names (separated by |) of the parents and its tenant name
    /// 2. TenantName - the last name in FullTenantName
    /// The FullTenantName is saved to the database, but the TenantName is derived from the FullTenantName
    /// </summary>
    public class Tenant : INameToShowOnException
    {
#pragma warning disable 649
        // ReSharper disable once CollectionNeverUpdated.Local
        private HashSet<Tenant> _children; //filled in by EF Core
        private HashSet<RoleToPermissions> _tenantRoles;
#pragma warning restore 649

        private Tenant() { } //Needed by EF Core

        private Tenant(string tenantFullName, bool isHierarchical, Tenant parent = null)
        {
            TenantFullName = tenantFullName?.Trim() ?? throw new ArgumentNullException(nameof(tenantFullName));

            //Hierarchical Tenant parts
            if (isHierarchical)
            {
                IsHierarchical = isHierarchical;
                ParentDataKey = parent?.GetTenantDataKey();
                Parent = parent;
            }
        }

        /// <summary>
        /// This defines a tenant in a single tenant multi-tenant system.
        /// </summary>
        /// <param name="fullTenantName">Name of the tenant</param>
        /// <param name="localizeDefault">localization service</param>
        /// <param name="tenantRoles">Optional: add Roles that have a <see cref="RoleTypes"/> of
        ///     <see cref="RoleTypes.TenantAutoAdd"/> or <see cref="RoleTypes.TenantAdminAdd"/></param>
        public static IStatusGeneric<Tenant> CreateSingleTenant(string fullTenantName, 
            IDefaultLocalizer localizeDefault, List<RoleToPermissions> tenantRoles = null)
        {
            var newInstance = new Tenant(fullTenantName, false);
            var status = CheckRolesAreAllTenantRolesAndSetTenantRoles(tenantRoles, newInstance, localizeDefault);
            return status;
        }

        /// <summary>
        /// This creates a tenant in a hierarchical multi-tenant system with a parent/child relationships
        /// You MUST have parent loaded and has been written to the database
        /// </summary>
        /// <param name="fullTenantName">This must be the full tenant name, including the parent name</param>
        /// <param name="parent">Parent tenant - can be null if top level</param>
        /// <param name="localizeDefault">localization service</param>
        /// <param name="tenantRoles">Optional: add Roles that have a <see cref="RoleTypes"/> of
        /// <see cref="RoleTypes.TenantAutoAdd"/> or <see cref="RoleTypes.TenantAdminAdd"/></param>
        public static IStatusGeneric<Tenant> CreateHierarchicalTenant(string fullTenantName, Tenant parent,
            IDefaultLocalizer localizeDefault, List<RoleToPermissions> tenantRoles = null)
        {
            var newInstance = new Tenant(fullTenantName, true, parent);
            var status = CheckRolesAreAllTenantRolesAndSetTenantRoles(tenantRoles, newInstance, localizeDefault);
            return status;
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
        /// This is true if the tenant is hierarchical 
        /// </summary>
        public bool IsHierarchical { get; private set; }

        /// <summary>
        /// This is true if the tenant has its own database.
        /// This is used by single-level tenants to return true for the query filter
        /// Also provides a quick way to find out what databases are used and how many tenants are in each database
        /// </summary>
        public bool HasOwnDb { get; private set; }

        /// <summary>
        /// If sharding is turned on then this will contain the name of database data
        /// in the shardingsettings.json file. This must not be null.
        /// </summary>
        public string DatabaseInfoName { get; private set; } 

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
        /// This holds any Roles that have been specifically 
        /// </summary>
        public IReadOnlyCollection<RoleToPermissions> TenantRoles => _tenantRoles?.ToList();
        /// <summary>
        /// Easy way to see the tenant and its key
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{TenantFullName}: Key = {this.GetTenantDataKey()}";
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
        /// This allows you to change the sharding information for this tenant
        /// </summary>
        /// <param name="newDatabaseInfoName">This contains the name of database data in the shardingsettings.json file</param>
        /// <param name="hasOwnDb">true if it is the only tenant in its database</param>
        public void UpdateShardingState(string newDatabaseInfoName, bool hasOwnDb)
        {
            DatabaseInfoName = newDatabaseInfoName ?? throw new ArgumentNullException(nameof(newDatabaseInfoName));
            HasOwnDb = hasOwnDb;
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
        /// This will replace the current tenant roles with a new set of tenant roles
        /// </summary>
        /// <param name="tenantRoles"></param>
        /// <param name="localizeDefault">localization service</param>
        /// <exception cref="AuthPermissionsException"></exception>
        /// <exception cref="AuthPermissionsBadDataException"></exception>
        public IStatusGeneric UpdateTenantRoles(List<RoleToPermissions> tenantRoles, IDefaultLocalizer localizeDefault)
        {
            if (_tenantRoles == null)
                throw new AuthPermissionsException(
                    $"You must include the tenant's {nameof(TenantRoles)} in your query before you can add/remove an tenant role.");

            var status = new StatusGenericLocalizer<Tenant>(localizeDefault);
            return status.CombineStatuses(CheckRolesAreAllTenantRolesAndSetTenantRoles(tenantRoles, this, localizeDefault));
        }

        /// <summary>
        /// This moves the current tenant to a another tenant
        /// </summary>
        /// <param name="newParentTenant">Can be null if moving to top</param>
        /// <param name="getChangeData">This action is called at every tenant that is effected.
        /// These starts at the parent and then recursively works down the children.
        /// This allows you to obtains the previous DataKey, the new DataKey and the fullname of every tenant that was moved</param>
        public void MoveTenantToNewParent(Tenant newParentTenant,
            Action<(string oldDataKey, Tenant tenant)> getChangeData)
        {
            if (!IsHierarchical)
                throw new AuthPermissionsException("You can only move a hierarchical tenant to a new parent");
            if (Children == null)
                throw new AuthPermissionsException("The children must be loaded to move a hierarchical tenant");

            var oldDataKey = this.GetTenantDataKey();
            TenantFullName = CombineParentNameWithTenantName(ExtractEndLeftTenantName(this.TenantFullName), newParentTenant?.TenantFullName);
            Parent = newParentTenant;
            ParentDataKey = newParentTenant?.GetTenantDataKey();
            getChangeData((oldDataKey, this));

            RecursivelyChangeChildNames(this, Children, (parent, child) =>
            {
                var thisLevelTenantName = ExtractEndLeftTenantName(child.TenantFullName);
                child.TenantFullName = CombineParentNameWithTenantName(thisLevelTenantName, parent.TenantFullName);
                var previousDataKey = child.GetTenantDataKey();
                child.ParentDataKey = parent?.GetTenantDataKey();
                getChangeData?.Invoke((previousDataKey, child));
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
        /// This checks that the given roles have a <see cref="RoleToPermissions.RoleType"/> that can be added to a tenant.
        /// If no errors (and roles aren't null) the <see cref="_tenantRoles"/> collection is updated, otherwise the status is returned with errors
        /// </summary>
        /// <param name="tenantRoles">The list of roles to added/updated to <see param="thisTenant"/> instance. Can be null</param>
        /// <param name="thisTenant">the current instance of the tenant</param>
        /// <param name="localizeDefault">localization service</param>
        /// <exception cref="AuthPermissionsBadDataException"></exception>
        /// <returns>status, with the <see param="thisTenant"/> instance if no errors.</returns>
        private static IStatusGeneric<Tenant> CheckRolesAreAllTenantRolesAndSetTenantRoles(
            List<RoleToPermissions> tenantRoles, Tenant thisTenant, IDefaultLocalizer localizeDefault)
        {
            var status = new StatusGenericLocalizer<Tenant>(localizeDefault);
            status.SetResult(thisTenant);

            var badRoles = tenantRoles?
                .Where(x => x.RoleType != RoleTypes.TenantAutoAdd && x.RoleType != RoleTypes.TenantAdminAdd)
                .ToList() ?? new List<RoleToPermissions>();

            foreach (var badRole in badRoles)
            {
                status.AddErrorFormatted("RoleNotTenantRole".StaticClassLocalizeKey(typeof(Tenant), true),
                    $"The Role '{badRole.RoleName}' is not a tenant role, i.e. only roles with a {nameof(RoleToPermissions.RoleType)} of ",
                    $"{nameof(RoleTypes.TenantAutoAdd)} or {nameof(RoleTypes.TenantAdminAdd)} can be added to a tenant.");
            }

            if (status.HasErrors || tenantRoles == null)
                return status; 
            
            thisTenant._tenantRoles = new HashSet<RoleToPermissions>(tenantRoles);
            return status;
        }

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