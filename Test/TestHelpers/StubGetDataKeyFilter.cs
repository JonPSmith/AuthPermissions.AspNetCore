// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example4.ShopCode.DataKeyCode;
using Example4.ShopCode.EfCoreClasses.SupportTypes;

namespace Test.TestHelpers
{
    public class StubGetDataKeyFilter : IDataKeyFilter
    {
        public StubGetDataKeyFilter(string dataKey)
        {
            DataKey = dataKey;
        }

        public string DataKey { get; }
    }
}