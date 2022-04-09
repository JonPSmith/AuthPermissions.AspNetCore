// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.SetupCode
{
    /// <summary>
    /// The class used with the <see cref="IFindUserInfoService"/> service
    /// </summary>
    public class FindUserInfoResult
    {
        /// <summary>
        /// You provide UserId and UserName
        /// </summary>
        /// <param name="userId">required</param>
        /// <param name="userName">optional</param>
        public FindUserInfoResult(string userId, string userName)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            UserName = userName;
        }

        /// <summary>
        /// Found userId (can be null if user not found)
        /// </summary>
        public string UserId { get; }
        /// <summary>
        /// Found user name (optional)
        /// </summary>
        public string UserName { get; }
    }
}