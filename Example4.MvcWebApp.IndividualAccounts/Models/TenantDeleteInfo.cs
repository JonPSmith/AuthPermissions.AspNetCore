// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class TenantDeleteInfo
    {
        public TenantDeleteInfo(string message, List<(string fullTenantName, string dataKey)> dataDropped)
        {
            Message = message;
            DataDropped = dataDropped.OrderBy(x => x.fullTenantName).ToList();
        }

        public string Message { get; }
        public List<(string fullTenantName, string dataKey)> DataDropped { get; }
    }
}