// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.CommonCode;
using Microsoft.AspNetCore.Http;

namespace AuthPermissions.AspNetCore.Services
{

    /// <summary>
    /// This service is registered if a multi-tenant setup is defined <see cref="AuthPermissionsOptions.TenantType"/>
    /// </summary>
    public class GetDataKeyFromUser : IGetDataKeyFromUser
    {
        /// <summary>
        /// This will return the AuthP' DataKey claim. If no user, or no claim then returns null
        /// </summary>
        /// <param name="accessor"></param>
        public GetDataKeyFromUser(IHttpContextAccessor accessor)
        {
            DataKey = accessor.HttpContext?.User.GetAuthDataKeyFromUser();
        }

        /// <summary>
        /// The AuthP' DataKey, can be null.
        /// </summary>
        public string DataKey { get; }
    }
}