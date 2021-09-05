// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class TenantMoveInfo
    {
        public TenantMoveInfo(string message, List<(string previousDataKey, string newDataKey, string newFullName)> moveData)
        {
            Message = message;
            MoveData = moveData.OrderBy(x => x.newFullName).ToList();
        }

        public string Message { get; }
        public List<(string previousDataKey, string newDataKey, string newFullName)> MoveData { get; }
    }
}