// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.AdminCode;
using Example4.ShopCode.EfCoreCode;
using Microsoft.Extensions.Logging;
using TestSupport.EfHelpers;

namespace Test.StubClasses
{

    public class StubRetailTenantChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
    {
        private readonly RetailDbContext _context;
        private readonly ILogger<RetailTenantChangeService> _logger;

        public List<LogOutput> Logs { get; } = new List<LogOutput>();

        public StubRetailTenantChangeServiceFactory(RetailDbContext context)
        {
            _context = context;
            _logger = new LoggerFactory(
                new[] { new MyLoggerProviderActionOut(l => Logs.Add(l)) })
                .CreateLogger<RetailTenantChangeService>();
        }


        public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return new RetailTenantChangeService(_context, _logger);
        }
    }
}