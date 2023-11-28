// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.GetDataKeyCode;

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