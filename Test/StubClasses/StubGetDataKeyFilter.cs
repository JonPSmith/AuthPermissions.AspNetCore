// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.Services;

namespace Test.StubClasses
{
    public class StubGetDataKeyFilter : IGetDataKeyFromUser
    {
        public StubGetDataKeyFilter(string dataKey)
        {
            DataKey = dataKey;
        }

        public string DataKey { get; }
    }
}