// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.Factories;
using Example6.SingleLevelSharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestSupport.EfHelpers;

namespace Test.StubClasses;

public class StubChangeChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
{
    private readonly object _caller;
    private readonly ShardingSingleDbContext _context;

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