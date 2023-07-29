// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.SetupCode.Factories;
using Example3.InvoiceCode.EfCoreCode;
using Example6.SingleLevelSharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubChangeChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
{
    private readonly ShardingSingleDbContext _context;
    private readonly object _caller;

    public StubChangeChangeServiceFactory(ShardingSingleDbContext context, object caller)
    {
        _context = context;
        _caller = caller;
    }

    public List<LogOutput> Logs { get; set; } = new List<LogOutput>();

    public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
    {
        var builder = new DbContextOptionsBuilder<ShardingSingleDbContext>();
        builder.UseSqlServer(null);
        var logger = new LoggerFactory(
                new[] { new MyLoggerProviderActionOut(log => Logs.Add(log)) })
            .CreateLogger<ShardingTenantChangeService>();
        return new ShardingTenantChangeService(builder.Options, new StubGetSetShardingEntries(_caller), logger);
    }
}