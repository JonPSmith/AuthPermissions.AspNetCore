// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Example6.SingleLevelSharding.AppStart;
using Example6.SingleLevelSharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using TestSupport.EfHelpers;
using TestSupport.Helpers;

namespace Test.TestHelpers;

public class ShardingSingleLevelTenantChangeSqlServerSetup : IDisposable
{
    public AuthPermissionsDbContext AuthPContext { get; }

    public ShardingSingleDbContext MainContext { get; }
    public ShardingSingleDbContext OtherContext { get; }

    public ShardingSingleLevelTenantChangeSqlServerSetup(object caller)
    {
        var authOptions = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
            .UseSqlServer(caller.GetUniqueDatabaseConnectionString("authp"), dbOptions =>
                dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));
        EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions.UseExceptionProcessor(authOptions);
        AuthPContext = new AuthPermissionsDbContext(authOptions.Options);

        var shardingOptions = new DbContextOptionsBuilder<ShardingSingleDbContext>()
            .UseSqlServer("bad connection string", dbOptions =>
                dbOptions.MigrationsHistoryTable(StartupExtensions.ShardingSingleDbContextHistoryName)).Options;
        MainContext = new ShardingSingleDbContext(shardingOptions, new StubGetShardingData("Default Database", caller, AuthPContext));
        OtherContext = new ShardingSingleDbContext(shardingOptions, new StubGetShardingData("Other Database", caller, AuthPContext));

        AuthPContext.Database.EnsureClean();
        MainContext.Database.EnsureClean();
        OtherContext.Database.EnsureClean();
    }

    private class StubGetShardingData : IGetShardingDataFromUser
    {
        private readonly AuthPermissionsDbContext _context;

        public StubGetShardingData(string connectionName, object caller, AuthPermissionsDbContext context)
        {
            _context = context;
            ConnectionString = new StubGetSetShardingEntries(caller).FormConnectionString(connectionName)
                               ?? throw new NotImplementedException("Don't know that connection name");
            DataKey = connectionName == "Other Database"
                ? MultiTenantExtensions.DataKeyNoQueryFilter
                : ".1";
        }

        public string DataKey { get; }
        public string ConnectionString { get; }
    }




    public void Dispose()
    {
        AuthPContext?.Dispose();
        MainContext?.Dispose();
        OtherContext?.Dispose();
    }
}