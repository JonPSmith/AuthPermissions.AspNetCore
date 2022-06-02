// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Example5.MvcWebApp.AzureAdB2C.Models;

public class InviteUserSetup
{
    /// <summary>
    /// Email of the user you want to invite
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// This allows you to select the Roles that the invited user
    /// </summary>
    public List<string> RoleNames { get; set; }

    /// <summary>
    /// This provides all the Roles that the invited users can have
    /// </summary>
    public List<string> AllRoleNames { get; set; }
}