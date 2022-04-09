// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.SetupCode
{
    /// <summary>
    /// This service can be used to find the UserId and optionally the user's name
    /// </summary>
    public interface IFindUserInfoService
    {
        /// <summary>
        /// When adding a AuthUser to the AuthP database you might not know the UserId
        /// You can write a service that that can take the uniqueName of the AuthUser (normally the email)
        /// and return the UserId, and optionally the user name (Azure Active Directory has a user name)
        /// </summary>
        /// <param name="uniqueName">The unique name you provide in your AuthUser setup data</param>
        /// <returns>a class containing a UserIf and UserName property, or null if not found</returns>
        public Task<FindUserInfoResult> FindUserInfoAsync(string uniqueName);
    }
}