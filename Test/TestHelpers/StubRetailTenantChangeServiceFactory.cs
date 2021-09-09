// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.SetupCode.Factories;
using Example4.ShopCode.EfCoreCode;

namespace Test.TestHelpers
{

    public class StubRetailTenantChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
    {

        public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return new RetailTenantChangeService();
        }
    }
}