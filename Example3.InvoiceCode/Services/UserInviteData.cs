// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Example3.InvoiceCode.Services;

public class UserInviteData
{
    /// <summary>
    /// Email of the user we want to invite
    /// </summary>
    public string EmailOfJoiner { get; set; }

    /// <summary>
    /// Role names for the 
    /// </summary>
    public List<string> JoinerRoles { get; set; }
    public int? TenantId { get; set; }
    
}