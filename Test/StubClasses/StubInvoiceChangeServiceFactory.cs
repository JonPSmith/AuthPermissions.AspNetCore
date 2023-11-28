// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.AdminCode;
using AuthPermissions.Factories;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.Extensions.Logging;
using TestSupport.EfHelpers;

namespace Test.StubClasses;

public class StubInvoiceChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
{
    private readonly InvoicesDbContext _context;

    public StubInvoiceChangeServiceFactory(InvoicesDbContext context)
    {
        _context = context;
    }

    public List<LogOutput> Logs { get; set; } = new List<LogOutput>();

    public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
    {
        var logger = new LoggerFactory(
                new[] { new MyLoggerProviderActionOut(log => Logs.Add(log)) })
            .CreateLogger<InvoiceTenantChangeService>();
        return new InvoiceTenantChangeService(_context, logger);
    }
}