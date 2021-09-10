// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Test.TestHelpers
{

    public class StubITenantChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
    {
        private readonly DbContext _appContext;
        private readonly string _errorMessage;

        public List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> MoveReturnedTuples =
            new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>();

        public StubITenantChangeServiceFactory(DbContext appContext, string errorMessage = null)
        {
            _appContext = appContext;
            _errorMessage = errorMessage;
        }

        public class StubITenantChangeService : ITenantChangeService
        {
            private readonly DbContext _appContext;
            private readonly StubITenantChangeServiceFactory _factory;
            private readonly string _errorMessage;

            public List<(string dataKey, string fullTenantName)> DeleteReturnedTuples { get; } =
                new List<(string fullTenantName, string dataKey)>();

            public StubITenantChangeService(DbContext appContext, StubITenantChangeServiceFactory factory, string errorMessage)
            {
                _appContext = appContext;
                _factory = factory;
                _errorMessage = errorMessage;
            }

            public DbContext GetNewInstanceOfAppContext(SqlConnection sqlConnection)
            {
                return _appContext;
            }

            public Task<string> HandleTenantDeleteAsync(DbContext appTransactionContext, string dataKey, int tenantId,
                string fullTenantName)
            {
                DeleteReturnedTuples.Add((fullTenantName, dataKey));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> HandleUpdateNameAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
            {
                return Task.FromResult(_errorMessage);
            }

            public Task<string> MoveTenantDataAsync(DbContext appTransactionContext, string oldDataKey, string newDataKey, int tenantId,
                string newFullTenantName)
            {
                _factory.MoveReturnedTuples.Add((oldDataKey, newDataKey, tenantId, newFullTenantName));

                return Task.FromResult(_errorMessage);
            }
        }

        public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return new StubITenantChangeService(_appContext, this, _errorMessage);
        }
    }
}