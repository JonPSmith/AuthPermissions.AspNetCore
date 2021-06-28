// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;

namespace Test.TestHelpers
{

    public class StubIFindUserInfoFactory : IFindUserInfoServiceFactory
    {
        private readonly bool _returnNullService;

        public StubIFindUserInfoFactory(bool returnNullService)
        {
            _returnNullService = returnNullService;
        }


        public IFindUserInfoService GetOptionalService()
        {
            return _returnNullService ? null : new StubIFindUserInfo();
        }

        public class StubIFindUserInfo : IFindUserInfoService
        {
            public Task<FindUserInfoResult> FindUserInfoAsync(string uniqueName)
            {
                return Task.FromResult(new FindUserInfoResult(uniqueName, null));
            }
        }
    }
}