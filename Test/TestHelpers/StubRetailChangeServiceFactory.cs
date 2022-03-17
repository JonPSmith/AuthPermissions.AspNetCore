// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.AdminCode;
using AuthPermissions.SetupCode.Factories;
using Example4.ShopCode.EfCoreCode;
using Microsoft.Extensions.Logging;
using TestSupport.EfHelpers;

namespace Test.TestHelpers;

public class StubRetailChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
{
    private readonly RetailDbContext _context;

    public StubRetailChangeServiceFactory(RetailDbContext context)
    {
        _context = context;
    }

    public List<LogOutput> Logs { get; set; } = new List<LogOutput>();

    public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
    {
        var logger = new LoggerFactory(
                new[] { new MyLoggerProviderActionOut(log => Logs.Add(log)) })
            .CreateLogger<RetailTenantChangeService>();
        return new RetailTenantChangeService(_context, logger);
    }
}