// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.SupportCode.AddUsersServices;

/// <summary>
/// This is used holds the data to securely add a new user to a AuthP application
/// </summary>
public class AddNewUserDto
{
    private string _email;
    private string _userName;

    /// <summary>
    /// Contains a unique Email (normalized by applying .ToLower), which is used for lookup
    /// If null, then it takes the UserName value
    /// </summary>
    [MaxLength(AuthDbConstants.EmailSize)]
    public string Email
    {
        get => _email?.Trim().ToLower() ?? _userName;
        set => _email = value;
    }

    /// <summary>
    /// Contains a unique user name
    /// This is used to a) provide more info on the user, or b) when using Windows authentication provider
    /// If null, then it takes non-normalized Email
    /// </summary>
    [MaxLength(AuthDbConstants.UserNameSize)]
    public string UserName
    {
        get => _userName ?? _email;
        set => _userName = value;
    }

    /// <summary>
    /// A list of Role names to add to the AuthP user when the AuthP user is created
    /// </summary>
    public List<string> Roles { get; set; }

    /// <summary>
    /// Optional. This holds the tenantId of the tenant that the joining user should be linked to
    /// If null, then the application must not be a multi-tenant application 
    /// </summary>
    public int? TenantId { get; set; }

    //----------------------------------------------------
    //If using a register / login authentication provider

    /// <summary>
    /// If using a register / login authentication provider you need to provide the user's password
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// If using a register / login authentication provider and using cookies
    /// setting this to true will make the cookie persist after using the app
    /// </summary>
    public bool IsPersistent { get; set; }

    /// <summary>
    /// This converts the list of roles into 
    /// </summary>
    /// <returns></returns>
    public string GetRolesAsCommaDelimited()
    {
        return string.Join(",", Roles?.Select(x => x.Trim()) ?? Array.Empty<string>());
    }
    
}