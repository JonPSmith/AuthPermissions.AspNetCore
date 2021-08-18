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
    /// This class is used to display/change the AuthUser
    /// </summary>
    public class SyncAuthUserWithChange
    {
        /// <summary>
        /// Ctor for sending back the data
        /// </summary>
        public SyncAuthUserWithChange () {}

        /// <summary>
        /// Ctor used by sync code to build the sync change data
        /// </summary>
        /// <param name="authenticationUser"></param>
        /// <param name="authUser"></param>
        internal SyncAuthUserWithChange(SyncAuthenticationUser authenticationUser, AuthUser authUser)
        {
            if (authUser != null)
            {
                UserId = authUser.UserId;
                OldEmail = authUser.Email;
                OldUserName = authUser.UserName;

                RoleNames = authUser.UserRoles.Select(x => x.RoleName).ToList();
                TenantName = authUser.UserTenant?.TenantFullName;
            }

            if (authenticationUser != null)
            {
                UserId = authenticationUser.UserId;
                Email = authenticationUser.Email;
                //Special handling of username
                //If the authenticationUser's UserName is same as its Email (or null), and the AuthUser has a value then don't update
                UserName = authenticationUser.UserName == null || (authenticationUser.UserName == authenticationUser.Email && OldUserName != null)
                    ? OldUserName
                    : authenticationUser.UserName;
            }

            //Now work out what the change is
            if (Email == OldEmail &&  UserName == OldUserName)
                FoundChange = SyncAuthUserChanges.NoChange;
            else if (authenticationUser == null)
                FoundChange = SyncAuthUserChanges.Delete;
            else if (authUser == null)
                FoundChange = SyncAuthUserChanges.Create;
            else
                FoundChange = SyncAuthUserChanges.Update;
        }

        /// <summary>
        /// This is set to the difference between authentication provider's user and the AuthPermission's AuthUser
        /// </summary>
        public SyncAuthUserChanges FoundChange { get; set; }

        /// <summary>
        /// The userId of the user (NOTE: this is not show 
        /// </summary>
        public string UserId { get;  set; }
        /// <summary>
        /// The user's main email (used as one way to find the user) 
        /// </summary>
        public string Email { get; set; }
        public string OldEmail { get; set; }
        public bool EmailChanged => Email != OldEmail;
        /// <summary>
        /// The user's name
        /// </summary>
        public string UserName { get; set; }
        public string OldUserName { get; set; }

        public bool UserNameChanged => UserName != OldUserName;

        //---------------------------------------------------
        //Auth parts

        /// <summary>
        /// The AuthRoles for this AuthUser
        /// </summary>
        public List<string> RoleNames { set; get; }

        public string NumRoles => RoleNames == null ? "not set" : RoleNames.Count.ToString();

        /// <summary>
        /// The name of the AuthP Tenant for this AuthUser (can be null)
        /// </summary>
        public string TenantName { set; get; }

        public bool HasTenant => !string.IsNullOrEmpty(TenantName);

        //---------------------------------------------------

        /// <summary>
        /// Summary to 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (FoundChange)
            {
                case SyncAuthUserChanges.NoChange:
                    throw new AuthPermissionsException("Shouldn't have this in the list");
                case SyncAuthUserChanges.Create:
                    return $"CREATE: Email = {Email}, UserName = {UserName}";
                case SyncAuthUserChanges.Update:
                    return $"UPDATE: Email {(EmailChanged ? "CHANGED" : "same")}, UserName {(UserNameChanged ? "CHANGED" : "same")}";
                case SyncAuthUserChanges.Delete:
                    return $"DELETE: OldEmail = {OldEmail}, OldUserName = {OldUserName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}