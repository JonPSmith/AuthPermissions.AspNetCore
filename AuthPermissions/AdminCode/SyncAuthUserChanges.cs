// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// The type of changes between the authentication provider's user and the AuthPermission's AuthUser
    /// Also used to confirm that the change should be made 
    /// </summary>
    public enum SyncAuthUserChanges
    {
        /// <summary>
        /// Ignore this change - can be set by the user
        /// </summary>
        NoChange,
        /// <summary>
        /// A new authentication provider's user, need to add a AuthP user  
        /// </summary>
        Create,
        /// <summary>
        /// The authentication provider user's email and/or username has change, so update AuthP user
        /// </summary>
        Update,
        /// <summary>
        /// A user has been removed from authentication provider' database, so delete AuthP user too
        /// </summary>
        Delete
    }
}