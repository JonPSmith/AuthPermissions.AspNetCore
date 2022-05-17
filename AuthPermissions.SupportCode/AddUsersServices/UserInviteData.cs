// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This is used holds the data to securely add a new user to a AuthP application
/// </summary>
public class UserInviteData
{
    /// <summary>
    /// Email of the user we want to invite
    /// </summary>
    public string EmailOfJoiner { get; set; }

    /// <summary>
    /// A list of Role names to add to the AuthP user when the joining user is created
    /// </summary>
    public List<string> JoinerRoles { get; set; }

    /// <summary>
    /// Optional. This holds the tenantId of the tenant that the joining user should be linked to
    /// If null, then the application must not be a multi-tenant application 
    /// </summary>
    public int? TenantId { get; set; }
    
}