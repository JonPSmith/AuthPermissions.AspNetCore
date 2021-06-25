// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// The type of changes between the authentication provider's user and the AuthPermission's AuthUser
    /// Also used to confirm that the change should be made 
    /// </summary>
    public enum SyncAuthUserChanges {NoChange, Add, Update, Remove}

    /// <summary>
    /// This class is used to display/change the AuthUser
    /// </summary>
    public class SyncAuthUserWithChange
    {
        public SyncAuthUserWithChange () {}

        public SyncAuthUserWithChange(SyncAuthenticationUser authenticationUser, AuthUser authUser)
        {
            if (authenticationUser != null & authUser != null)
            {
                if (authenticationUser.Email == authUser.Email &&
                    authenticationUser.UserName == authUser.UserName)
                {
                    ProviderChange = SyncAuthUserChanges.NoChange;
                    return;
                }
                ProviderChange = SyncAuthUserChanges.Update;
            }

            if (authenticationUser == null)
                ProviderChange = SyncAuthUserChanges.Remove;
            else if (authUser == null)
                ProviderChange = SyncAuthUserChanges.Add;

            ConfirmChange = ProviderChange;

            if (authenticationUser != null)
            {
                UserId = authenticationUser.UserId;
                Email = authenticationUser.Email;
                UserName = authenticationUser.UserName;
            }
            if (authUser != null)
            {
                UserId = authUser.UserId;
                OldEmail = authUser.Email;
                OldUserName = authUser.UserName;

                RoleNames = authUser.UserRoles.Select(x => x.RoleName).ToList();
                TenantName = authUser.UserTenant?.TenantName;
            }
        }

        /// <summary>
        /// This is set to the difference between authentication provider's user and the AuthPermission's AuthUser
        /// </summary>
        public SyncAuthUserChanges ProviderChange { get; set; }

        /// <summary>
        /// This is set by the admin person
        /// </summary>
        public SyncAuthUserChanges ConfirmChange { get; set; }

        /// <summary>
        /// The userId of the user (NOTE: this is not show 
        /// </summary>
        public string UserId { get;  set; }
        /// <summary>
        /// The user's main email (used as one way to find the user) 
        /// </summary>
        public string Email { get; set; }
        public string OldEmail { get; set; }
        /// <summary>
        /// The user's name
        /// </summary>
        public string UserName { get; set; }
        public string OldUserName { get; set; }

        //---------------------------------------------------
        //Auth parts

        /// <summary>
        /// The AuthRoles for this AuthUser
        /// </summary>
        public List<string> RoleNames { set; get; }

        /// <summary>
        /// The name of the AuthP Tenant for this AuthUser (can be null)
        /// </summary>
        public string TenantName { set; get; }

        //---------------------------------------------------

        /// <summary>
        /// Useful summary for debugging
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (ProviderChange)
            {
                case SyncAuthUserChanges.NoChange:
                    throw new AuthPermissionsException("Shouldn't have this in the list");
                case SyncAuthUserChanges.Add:
                    return $"ADD: Email = {Email}, UserName = {UserName}"; ;
                case SyncAuthUserChanges.Update:
                    return $"UPDATE: Email {(Email == OldEmail ? "CHANGED" : "same")}, UserName {(UserName == OldUserName ? "CHANGED" : "same")}";
                case SyncAuthUserChanges.Remove:
                    return $"REMOVE: OldEmail = {OldEmail}, OldUserName = {OldUserName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}