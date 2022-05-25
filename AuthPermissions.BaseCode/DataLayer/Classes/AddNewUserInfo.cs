// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.BaseCode.DataLayer.Classes;

/// <summary>
/// This class is used to hold the data needed to set up an AuthUser
/// when it has to be done within the login event
/// </summary>
public class AddNewUserInfo : INameToShowOnException
{
    private sealed class AddNewUserInfoEqualityComparer : IEqualityComparer<AddNewUserInfo>
    {
        public bool Equals(AddNewUserInfo x, AddNewUserInfo y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Email == y.Email && x.UserName == y.UserName && x.RolesNamesCommaDelimited == y.RolesNamesCommaDelimited && x.TenantId == y.TenantId;
        }

        public int GetHashCode(AddNewUserInfo obj)
        {
            return HashCode.Combine(obj.Email, obj.UserName, obj.RolesNamesCommaDelimited, obj.TenantId);
        }
    }

    public static IEqualityComparer<AddNewUserInfo> AddNewUserInfoComparer { get; } = new AddNewUserInfoEqualityComparer();

    /// <summary>
    /// This creates an AddNewUserInfo as used to add to authentication handlers that
    /// don't allow you to register a user before they log in.
    /// This is used by the "invite user" and "sign on" features
    /// </summary>
    /// <param name="email"></param>
    /// <param name="userName"></param>
    /// <param name="rolesNamesCommaDelimited"></param>
    /// <param name="tenantId"></param>
    public AddNewUserInfo(string email, string userName, string rolesNamesCommaDelimited, int? tenantId)
    {
        Email = email?.Trim().ToLower() ?? userName;
        UserName = userName ?? email?.Trim();
        RolesNamesCommaDelimited = rolesNamesCommaDelimited;
        TenantId = tenantId;

        if (Email == null || UserName == null)
            throw new AuthPermissionsBadDataException("The Email or UserName is null.");
    }


    /// <summary>
    /// Contains a unique Email, which is used for lookup
    /// If Email is null, then it contains the UserName
    /// </summary>
    [MaxLength(AuthDbConstants.EmailSize)]
    public string Email { get; private set; }

    /// <summary>
    /// Contains a unique user name
    /// This is used to a) provide more info on the user, or b) when using Windows authentication provider
    /// If UserName is null, then it contains the Email
    /// </summary>
    [MaxLength(AuthDbConstants.UserNameSize)]
    public string UserName { get; private set; }

    /// <summary>
    /// This contains a list Roles to be added to the AuthUser is created
    /// </summary>
    public string RolesNamesCommaDelimited { get; private set; }

    /// <summary>
    /// The tenantId is used to set the tenant that the user is linked to (can be null)
    /// </summary>
    public int? TenantId { get; private set; }

    /// <summary>
    /// Date when created
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    //--------------------------------------------------
    // Exception Error name

    /// <summary>
    /// Used when there is an exception
    /// </summary>
    public string NameToUseForError
    {
        get
        {
            if (Email != null && UserName != null && Email != UserName)
                //If both the Email and the UserName are set, and aren't the same we show both
                return $"{Email} or {UserName}";

            return UserName ?? Email;
        }
    }
}