// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.SetupCode.Factories;

namespace Test.TestHelpers
{
    public class StubSyncAuthenticationUsersFactory : IAuthPServiceFactory<ISyncAuthenticationUsers>
    {
        private readonly bool _returnNullService;

        public StubSyncAuthenticationUsersFactory(bool returnNullService = false)
        {
            _returnNullService = returnNullService;
        }

        public class StubSyncAuthenticationUsers : ISyncAuthenticationUsers
        {
            public Task<IEnumerable<SyncAuthenticationUser>> GetAllActiveUserInfoAsync()
            {
                var result = new List<SyncAuthenticationUser>
                {
                    new SyncAuthenticationUser( "User1", "User1@gmail.com", "first last 0"), //No change
                    new SyncAuthenticationUser("User2", "User2@NewGmail.com", "new name"), //change of email and username
                    new SyncAuthenticationUser("User99", "User99@gmail.com", "user 99"), //create new
                };

                return Task.FromResult(result.AsEnumerable());
            }
        }

        public ISyncAuthenticationUsers GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return _returnNullService ? null : new StubSyncAuthenticationUsers();
        }
    }
}