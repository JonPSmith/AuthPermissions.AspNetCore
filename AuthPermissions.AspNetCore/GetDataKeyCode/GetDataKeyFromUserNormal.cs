// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.CommonCode;
using Microsoft.AspNetCore.Http;

namespace AuthPermissions.AspNetCore.GetDataKeyCode
{

    /// <summary>
    /// This service is registered if a multi-tenant setup is defined <see cref="AuthPermissionsOptions.TenantType"/>
    /// NOTE: There is a <see cref="GetDataKeyFromUserAccessTenantData"/> version if the "Access the data of other tenant" is turned on
    /// </summary>
    public class GetDataKeyFromUserNormal : IGetDataKeyFromUser
    {
        /// <summary>
        /// This will return the AuthP' DataKey claim. If no user, or no claim then returns null
        /// </summary>
        /// <param name="accessor"></param>
        public GetDataKeyFromUserNormal(IHttpContextAccessor accessor)
        {
            DataKey = accessor.HttpContext?.User.GetAuthDataKeyFromUser();
        }

        /// <summary>
        /// The AuthP' DataKey, can be null.
        /// </summary>
        public string DataKey { get; }
    }
}