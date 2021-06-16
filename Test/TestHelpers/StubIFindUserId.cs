// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.SetupCode;

namespace Test.TestHelpers
{
    public class StubIFindUserId : IFindUserIdService
    {
        public Task<string> FindUserIdAsync(string uniqueName)
        {
            return Task.FromResult(uniqueName);
        }
    }
}