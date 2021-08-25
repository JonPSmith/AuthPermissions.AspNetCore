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
    /// This class is used to find, display and change the AuthUser
    /// The <see cref="IAuthUsersAdminService.SyncAndShowChangesAsync"/> method uses
    /// the internal constructor to work out what has changed
    /// The public constructor is used when a view/page/WebAPI wants to return these changes.
    /// </summary>
    public class SyncAuthUserWithChange
    {
        /// <summary>
        /// Ctor for sending back the data
        /// </summary>
        public SyncAuthUserWithChange () {}

        /// <summary>
        /// Ctor used by sync code to build the sync change data
        /// In general the following happens 
        /// - OldEmail and OldUserName contain the values from the AuthUser
        /// - Email and UserName contain the values from the authentication provider Uses
        /// The exception is Delete, where we want to show the AuthUser data. In this case
        /// -  Email and UserName contain the values from the AuthUser
        /// - OldEmail and OldUserName are set to null, which marks them changed
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
                UserId = authenticationUser.UserId; //Notice that if authenticationUser != null it overrides the 
                Email = authenticationUser.Email;
                //Special handling of username
                //If the authenticationUser's UserName is same as its Email (or null), and the AuthUser has a value then don't update
                UserName = authenticationUser.UserName == null || (authenticationUser.UserName == authenticationUser.Email && OldUserName != null)
                    ? OldUserName
                    : authenticationUser.UserName;
            }

            //Now work out what the change is
            if (Email == OldEmail &&  UserName == OldUserName)
                FoundChangeType = SyncAuthUserChangeTypes.NoChange;
            else if (authenticationUser == null)
            {
                FoundChangeType = SyncAuthUserChangeTypes.Delete;
                //Need to set the Email and UserName so that can show the AuthP user's values
                Email = authUser.Email;
                UserName = authUser.UserName;
                //And set old version to null so it shows the changes
                OldEmail = null;
                OldUserName = null;
            }
            else if (authUser == null)
                FoundChangeType = SyncAuthUserChangeTypes.Create;
            else
                FoundChangeType = SyncAuthUserChangeTypes.Update;
        }

        /// <summary>
        /// This is set to the difference between authentication provider's user and the AuthPermission's AuthUser
        /// </summary>
        public SyncAuthUserChangeTypes FoundChangeType { get; set; }

        /// <summary>
        /// The userId of the user (NOTE: this is not shown) 
        /// </summary>
        public string UserId { get;  set; }
        /// <summary>
        /// The user's main email (used as one way to find the user) 
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Holds the AuthP version
        /// </summary>
        public string OldEmail { get; set; }
        /// <summary>
        /// True if Emails are different
        /// </summary>
        public bool EmailChanged => Email != OldEmail;
        /// <summary>
        /// The user's name
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Holds the AuthP version
        /// </summary>
        public string OldUserName { get; set; }
        /// <summary>
        /// True if usernames different
        /// </summary>
        public bool UserNameChanged => UserName != OldUserName;

        //---------------------------------------------------
        //Auth parts

        /// <summary>
        /// The AuthRoles for this AuthUser
        /// </summary>
        public List<string> RoleNames { set; get; }

        /// <summary>
        /// Number of roles, or "not set" if none
        /// </summary>
        public string NumRoles => RoleNames == null ? "not set" : RoleNames.Count.ToString();

        /// <summary>
        /// The name of the AuthP Tenant for this AuthUser (can be null)
        /// </summary>
        public string TenantName { set; get; }

        /// <summary>
        /// True if the user has a tenant
        /// </summary>
        public bool HasTenant => !string.IsNullOrEmpty(TenantName);

        //---------------------------------------------------

        /// <summary>
        /// Summary to 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (FoundChangeType)
            {
                case SyncAuthUserChangeTypes.NoChange:
                    throw new AuthPermissionsException("Shouldn't have this in the list");
                case SyncAuthUserChangeTypes.Create:
                    return $"CREATE: Email = {Email}, UserName = {UserName}";
                case SyncAuthUserChangeTypes.Update:
                    return $"UPDATE: Email {(EmailChanged ? "CHANGED" : "same")}, UserName {(UserNameChanged ? "CHANGED" : "same")}";
                case SyncAuthUserChangeTypes.Delete:
                    return $"DELETE: Email = {Email}, UserName = {UserName}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}