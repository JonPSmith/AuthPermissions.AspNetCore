// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.AspNetCore.Http;

namespace AuthPermissions.AspNetCore.GetDataKeyCode
{

    /// <summary>
    /// This service is registered if a multi-tenant setup with sharding on
    /// NOTE: There are other versions if the "Access the data of other tenant" is turned on
    /// </summary>
    public class GetShardingDataUserNormal : IGetShardingDataFromUser
    {
        /// <summary>
        /// This will return the AuthP's DataKey and the connection string via the ConnectionName claim.
        /// If no user, or no claim then both parameters will be null
        /// </summary>
        /// <param name="accessor"></param>
        /// <param name="connectionService">Service to get the current connection string for the  </param>
        public GetShardingDataUserNormal(IHttpContextAccessor accessor, IGetSetShardingEntries connectionService)
        {
            DataKey = accessor.HttpContext?.User.GetAuthDataKeyFromUser();
            var databaseDataName = accessor.HttpContext?.User.GetDatabaseInfoNameFromUser();
            if (databaseDataName != null)
                ConnectionString = connectionService.FormConnectionString(databaseDataName);
        }

        /// <summary>
        /// The AuthP' DataKey, can be null.
        /// </summary>
        public string DataKey { get; }

        /// <summary>
        /// This contains the connection string to the database to use
        /// If null, then use the default connection string as defined at the time when your application's DbContext was registered
        /// </summary>
        public string ConnectionString { get; }
    }
}