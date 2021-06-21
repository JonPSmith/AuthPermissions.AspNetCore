// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.SetupCode
{
    public class FindUserInfoResult
    {
        public FindUserInfoResult(string userId, string userName)
        {
            UserId = userId;
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