// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.SetupCode;

namespace Test.TestHelpers
{
    public class StubIFindUserInfo : IFindUserInfoService
    {
        public Task<FindUserInfoResult> FindUserInfoAsync(string uniqueName)
        {
            return Task.FromResult(new FindUserInfoResult(uniqueName, null));
        }
    }
}