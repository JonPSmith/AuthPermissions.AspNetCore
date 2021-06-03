// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace AuthPermissions.CommonCode
{
    /// <summary>
    /// A AuthPermissions for bad data errors
    /// </summary>
    public class AuthPermissionsBadDataException : ArgumentException
    {
        /// <summary>
        /// Just send a message
        /// </summary>
        /// <param name="message"></param>
        public AuthPermissionsBadDataException(string message)
            : base(message) {}

        /// <summary>
        /// Message and parameter name
        /// </summary>
        /// <param name="message"></param>
        /// <param name="paramName"></param>
        public AuthPermissionsBadDataException(string message, string paramName)
            : base(message, paramName) { }
    }
}