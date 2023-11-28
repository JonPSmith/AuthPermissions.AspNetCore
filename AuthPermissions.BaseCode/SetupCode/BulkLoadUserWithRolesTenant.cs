// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.BaseCode.SetupCode;

/// <summary>
/// Class used to define users with their roles and tenant
/// </summary>
public class BulkLoadUserWithRolesTenant
{
    /// <summary>
    /// This defines a user in your application
    /// </summary>
    /// <param name="email">Unique email</param>
    /// <param name="userName">name to help the admin team to work out who the user is</param>
    /// <param name="roleNamesCommaDelimited">A string containing a comma delimited set of auth roles that the user</param>
    /// <param name="userId">If null, then you must register a <see cref="IFindUserInfoService"/> to provide a lookup of the UserId</param>
    /// <param name="uniqueUserName">A string that is unique for each user, e.g. email. If not provided then uses the userName</param>
    /// <param name="tenantNameForDataKey">Optional: The unique name of your multi-tenant that this user is linked to</param>
    public BulkLoadUserWithRolesTenant(string email, string userName, string roleNamesCommaDelimited,
        string userId = null,
        string uniqueUserName = null, string tenantNameForDataKey = null)
    {
        UserId = userId; //Can be null
        Email = email ?? throw new ArgumentNullException(nameof(email));
        UserName = userName ?? email;
        RoleNamesCommaDelimited = roleNamesCommaDelimited ??
                                  throw new ArgumentNullException(nameof(roleNamesCommaDelimited));
        UniqueUserName = uniqueUserName ?? UserName;
        TenantNameForDataKey = tenantNameForDataKey;
    }

    /// <summary>
    /// This is what AuthPermissions needs to create a new AuthP User
    /// You can set the userId directly or if you leave it as null then you must provide <see cref="IFindUserInfoService"/>
    /// which will interrogate the authentication provider for the UserId
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Contains a name to help the admin team to work out who the user is
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Contains a name to help the admin team to work out who the user is
    /// </summary>
    [MaxLength(AuthDbConstants.UserNameSize)]
    public string UserName { get; }

    /// <summary>
    /// This contains a string that is unique for each user, e.g. email
    /// </summary>
    public string UniqueUserName { get; }

    /// <summary>
    /// This contains the Tenant name. Used to provide the user with a multi-tenant data key
    /// </summary>
    public string TenantNameForDataKey { get; }

    /// <summary>
    /// List of role names in a comma delimited list
    /// </summary>
    public string RoleNamesCommaDelimited { get;  }

    /// <summary>
    /// Useful when debugging
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{nameof(UserId)}: {UserId ?? "null"}, {nameof(Email)}: {Email}, {nameof(UserName)}: {UserName ?? "null"}, " +
               $"{nameof(TenantNameForDataKey)}: {TenantNameForDataKey ?? "null"}, {nameof(RoleNamesCommaDelimited)}: {RoleNamesCommaDelimited ?? "null"}";
    }
}