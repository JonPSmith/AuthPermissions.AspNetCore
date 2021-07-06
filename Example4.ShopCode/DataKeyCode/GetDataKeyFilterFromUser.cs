// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.CommonCode;
using Example4.ShopCode.EfCoreClasses.SupportTypes;
using Microsoft.AspNetCore.Http;

namespace Example4.ShopCode.DataKeyCode
{
    public class GetDataKeyFilterFromUser : IDataKeyFilter
    {
        public GetDataKeyFilterFromUser(IHttpContextAccessor accessor)
        {
            DataKey = accessor.HttpContext?.User.GetAuthDataKeyFromUser();
        }

        public string DataKey { get; }
    }
}