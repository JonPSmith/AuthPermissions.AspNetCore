// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ExamplesCommonCode.CommonAdmin;

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

    /// <summary>
    /// This holds the selected invite expiration time in ticks. If default, then no  
    /// </summary>
    public long InviteExpiration { get; set; }

    /// <summary>
    /// This contains key/value dropdown data for setting an expiration time on a invite.
    /// See <see cref="InviteNewUserService.ListOfExpirationTimes"/> static method for a set of times
    /// </summary>
    public List<KeyValuePair<long, string>> ExpirationTimesDropdown { get; set; }
}