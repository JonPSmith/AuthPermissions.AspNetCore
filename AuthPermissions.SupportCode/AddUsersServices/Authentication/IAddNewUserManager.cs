// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

/// <summary>
/// This defines the properties / methods use to add a new user to an application that us using AuthP
/// This means it much easier for you to use the "invite user" and "sign up" features with any authentication
/// There are two implementation of this interface cover nearly all the normal authentication handlers
/// 1. <see cref="IndividualUserAddUserManager{TIdentity}"/>, which works with the Individual User Accounts
/// 2. <see cref="AzureAdNewUserManager"/>, which works for Azure AD
/// </summary>
public interface IAddNewUserManager
{
    /// <summary>
    /// This tells you what Authentication handler, or group of handlers, that the Add User Manager supports
    /// </summary>
    string AuthenticationGroup { get; }

    /// <summary>
    /// This holds the data provided for the login.
    /// Used to check that the email of the person who will login is the same as the email provided by the user
    /// NOTE: Email and UserName can be null if providing a default value
    /// </summary>
    AddNewUserDto UserLoginData { get; }

    /// <summary>
    /// This makes a quick check that the user isn't already has an AuthUser 
    /// </summary>
    /// <param name="newUser"></param>
    /// <returns>status, with error if there an user already</returns>
    Task<IStatusGeneric> CheckNoExistingAuthUserAsync(AddNewUserDto newUser);

    /// <summary>
    /// This either register the user and creates the AuthUser to match, or for
    /// external authentication handlers where you can't get a user's data before the login 
    /// it adds the new user AuthP information into the database to be read within the login event
    /// </summary>
    /// <param name="newUser">The information for creating an AuthUser </param>
    /// <returns>Returns the user Id</returns>
    Task<IStatusGeneric<AuthUser>> SetUserInfoAsync(AddNewUserDto newUser);

    /// <summary>
    /// Optional: this logs in the user if the authentication handler can do that
    /// </summary>
    /// <returns>status with the final <see cref="AddNewUserDto"/> setting.
    /// This is needed in the Azure AD version, as it creates a temporary password.</returns>
    Task<IStatusGeneric<AddNewUserDto>> LoginAsync();

    /// <summary>
    /// If something happens that makes the user invalid, then this will remove the AuthUser.
    /// Used in <see cref="SignInAndCreateTenant"/> if something goes wrong and we want to undo the tenant
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IStatusGeneric> RemoveAuthUserAsync(string userId);

}