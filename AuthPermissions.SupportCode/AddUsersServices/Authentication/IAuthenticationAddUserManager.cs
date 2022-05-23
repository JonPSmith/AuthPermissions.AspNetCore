// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using StatusGeneric;

namespace AuthPermissions.SupportCode.AddUsersServices.Authentication;

public interface IAuthenticationAddUserManager
{
    /// <summary>
    /// This holds the data provided for the login.
    /// Used to check that the email of the person who will login is the same as the email provided by the user
    /// NOTE: Email and UserName can be null if providing a default value
    /// </summary>
    AddUserData UserLoginData { get; }

    /// <summary>
    /// This checks if a user already exists with the given email / userName
    /// This is used to stop an AuthUser being registered again (which would fail) 
    /// </summary>
    /// <param name="email">email of the user. Can be null if userName is provided</param>
    /// <param name="userName">Optional username</param>
    /// <returns>returns true if there is no AuthP user with that email / username</returns>
    Task<bool> CheckNoAuthUserAsync(string email, string userName = null);

    /// <summary>
    /// This either register the user and creates the AuthUser to match, or for
    /// external authentication handlers where you can't get a user's data before the login 
    /// it adds the new user AuthP information into the database to be read within the login event
    /// </summary>
    /// <param name="userData">The information for creating an AuthUser </param>
    /// <param name="password">optional: If you have access to the users you can confirm their identity before creating an AuthUser</param>
    Task<IStatusGeneric> SetUserInfoAsync(AddUserData userData, string password = null);

    /// <summary>
    /// This logs in the user, checking that the email / username are the same as was provided
    /// </summary>
    /// <param name="givenEmail">email to login by</param>
    /// <param name="givenUserName">username to login by</param>
    /// <param name="isPersistent">true if cookie should be persistent</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> LoginUserWithVerificationAsync(string givenEmail, string givenUserName, bool isPersistent);
}