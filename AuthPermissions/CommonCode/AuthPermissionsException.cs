// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace AuthPermissions.CommonCode
{
    /// <summary>
    /// A AuthPermissions for internal errors
    /// </summary>
    public class AuthPermissionsException : Exception
    {
        public AuthPermissionsException(string message)
            : base(message)
        {}
    }
}