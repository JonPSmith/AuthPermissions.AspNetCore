// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;

namespace Example5.MvcWebApp.AzureAdB2C.AzureAdCode
{
    public class SyncAzureAdUsers : ISyncAuthenticationUsers
    {
        public Task<IEnumerable<SyncAuthenticationUser>> GetAllActiveUserInfoAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}