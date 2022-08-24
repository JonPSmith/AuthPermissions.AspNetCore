// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.SetupCode.Factories;

namespace Test.StubClasses
{

    public class StubITenantChangeServiceFactory : IAuthPServiceFactory<ITenantChangeService>
    {
        private readonly string _errorMessage;

        public string NewTenantName { get; set; }

        public List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> MoveReturnedTuples = new();


        public StubITenantChangeServiceFactory(string errorMessage = null)
        {
            _errorMessage = errorMessage;
        }


        public ITenantChangeService GetService(bool throwExceptionIfNull = true)
        {
            return new StubITenantChangeService(this, _errorMessage);
        }

        public class StubITenantChangeService : ITenantChangeService
        {
            private readonly StubITenantChangeServiceFactory _factory;
            private readonly string _errorMessage;

            public List<(string dataKey, string fullTenantName)> DeleteReturnedTuples { get; } = new();

            public StubITenantChangeService(StubITenantChangeServiceFactory factory, string errorMessage)
            {
                _factory = factory;
                _errorMessage = errorMessage;
            }

            public Task<string> CreateNewTenantAsync(Tenant tenant)
            {
                _factory.NewTenantName = tenant.TenantFullName;

                return Task.FromResult(_errorMessage);
            }

            public Task<string> SingleTenantUpdateNameAsync(Tenant tenant)
            {
                return Task.FromResult(_errorMessage);
            }

            public Task<string> HandleTenantDeleteAsync(string dataKey, int tenantId,
                string fullTenantName)
            {
                DeleteReturnedTuples.Add((fullTenantName, dataKey));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> SingleTenantDeleteAsync(Tenant tenant)
            {
                DeleteReturnedTuples.Add((tenant.GetTenantDataKey(), tenant.TenantFullName));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> HierarchicalTenantUpdateNameAsync(List<Tenant> tenantsToUpdate)
            {
                return Task.FromResult(_errorMessage);
            }

            public Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder)
            {
                DeleteReturnedTuples.AddRange(tenantsInOrder.Select(x => (x.TenantFullName, x.GetTenantDataKey())));

                return Task.FromResult(_errorMessage);
            }

            public Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, Tenant tenantToMove)> tenantToUpdate)
            {
                _factory.MoveReturnedTuples = tenantToUpdate.Select(x =>
                    (x.oldDataKey, x.tenantToMove.GetTenantDataKey(), x.tenantToMove.TenantId, x.tenantToMove.TenantFullName)
                ).ToList();
                return Task.FromResult(_errorMessage);
            }

            public Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName,
                string oldDataKey,
                Tenant updatedTenant)
            {
                return Task.FromResult(_errorMessage);
            }
        }

        public ITenantChangeService GetService(bool throwExceptionIfNull = true, string callingMethod = "")
        {
            return new StubITenantChangeService(this, _errorMessage);
        }
    }
}