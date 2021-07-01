// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class AuthIdAndChange
    {
        public SyncAuthUserChanges FoundChange { get; set; }
        public string UserId { get; set; }
    }
}