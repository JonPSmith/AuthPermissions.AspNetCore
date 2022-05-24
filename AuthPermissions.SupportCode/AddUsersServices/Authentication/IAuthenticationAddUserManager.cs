// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

/// <summary>
/// This defines the properties / methods use to add a new user to an application that us using AuthP
/// This means it much easier for you to use the "invite user" and "sign up" features with any authentication
/// There are two implementation of this interface cover nearly all the normal authentication handlers
/// 1. <see cref="IndividualUserAddUserManager{TIdentity}"/>, which works with the Individual User Accounts
/// 2. <see cref="NonRegisterAddUserManager"/>, which works for any authentication handler where you can't access the user list,
///    e.g., Social logins like Google, Twitter etc. NOTE: These need extra code that is called in a login event 
/// </summary>
public interface IAuthenticationAddUserManager
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
    AddUserDataDto UserLoginData { get; }

    /// <summary>
    /// This makes a quick check that the user isn't already has an AuthUser 
    /// </summary>
    /// <param name="userData"></param>
    /// <returns>status, with error if there an user already</returns>
    Task<IStatusGeneric> CheckNoExistingAuthUser(AddUserDataDto userData);

    /// <summary>
    /// This either register the user and creates the AuthUser to match, or for
    /// external authentication handlers where you can't get a user's data before the login 
    /// it adds the new user AuthP information into the database to be read within the login event
    /// </summary>
    /// <param name="userData">The information for creating an AuthUser </param>
    /// <param name="password">optional: If you have access to the users you can confirm their identity before creating an AuthUser</param>
    Task<IStatusGeneric> SetUserInfoAsync(AddUserDataDto userData, string password = null);

    /// <summary>
    /// This logs in the user, checking that the email / username are the same as was provided
    /// OR
    /// For non-register authentication handles it can only check that the new user has be added properly
    /// </summary>
    /// <param name="givenEmail">email to login by</param>
    /// <param name="givenUserName">username to login by</param>
    /// <param name="isPersistent">true if cookie should be persistent</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> LoginVerificationAsync(string givenEmail, string givenUserName, bool isPersistent = false);
}