// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.SetupCode.Factories;
using Microsoft.EntityFrameworkCore;

namespace Test.TestHelpers
{

    public class StubITenantChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
    {
        private readonly DbContext _appContext;
        private readonly string _errorMessage;

        public string NewTenantName { get; set; }

        public List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> MoveReturnedTuples =
            new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>();



        public StubITenantChangeServiceFactory(DbContext appContext, string errorMessage = null)
        {
            _appContext = appContext;
            _errorMessage = errorMessage;
        }

        public class StubITenantChangeService : ITenantChangeService
        {
            private readonly StubITenantChangeServiceFactory _factory;
            private readonly string _errorMessage;

            public List<(string dataKey, string fullTenantName)> DeleteReturnedTuples { get; } =
                new List<(string fullTenantName, string dataKey)>();

            public StubITenantChangeService(StubITenantChangeServiceFactory factory, string errorMessage)
            {
                _factory = factory;
                _errorMessage = errorMessage;
            }

            public Task<string> CreateNewTenantAsync(string dataKey, int tenantId, string fullTenantName)
            {
                _factory.NewTenantName = fullTenantName;

                return Task.FromResult(_errorMessage);
            }

            public Task<string> HandleTenantDeleteAsync(string dataKey, int tenantId,
                string fullTenantName)
            {
                DeleteReturnedTuples.Add((fullTenantName, dataKey));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> HandleUpdateNameAsync(string dataKey, int tenantId, string fullTenantName)
            {
                return Task.FromResult(_errorMessage);
            }

            public Task<string> SingleTenantDeleteAsync(string dataKey, int tenantId, string fullTenantName)
            {
                DeleteReturnedTuples.Add((dataKey, fullTenantName));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder)
            {
                DeleteReturnedTuples.AddRange( tenantsInOrder.Select(x => (x.TenantFullName, x.GetTenantDataKey())));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> tenantToUpdate)
            {
                _factory.MoveReturnedTuples = tenantToUpdate;
                return Task.FromResult(_errorMessage);
            }

        }

        public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return new StubITenantChangeService(this, _errorMessage);
        }
    }
}