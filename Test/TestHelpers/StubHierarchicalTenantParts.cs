// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.DataLayer.Classes;

namespace Test.TestHelpers
{
    public class StubTenantParts : ITenantPartsToExport
    {
        private readonly string _dataKey;

        public StubTenantParts(string fullName, string dataKey)
        {
            TenantFullName = fullName;
            _dataKey = dataKey;
        }

        public int TenantId { get; set; }
        public string TenantFullName { get; }
        public bool IsHierarchical { get; set; }
        public string GetTenantDataKey()
        {
            return _dataKey;
        }

        public string GetTenantEndLeafName()
        {
            return Tenant.ExtractEndLeftTenantName(TenantFullName);
        }
    }
}