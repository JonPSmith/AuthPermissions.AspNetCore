// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// This service allows you to mark the Jwt Refresh Token as 'used' so that the JWT token cannot be refreshed.
/// There are two methods: one to allow a user to logout and another to allow a admin person log out all logins of a specific user. 
/// </summary>
public interface IDisableJwtRefreshToken
{
    /// <summary>
    /// This allows a user to logout. The effect is that when the JWT Token expires
    /// you cannot refresh the JWT Token because the refresh token is invalid.
    /// If the user has multiple logins, this only logs out the login using the given refresh token.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    Task LogoutUserViaRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// This will mark all the refresh tokens linked to this userid will be marked as invalid,
    /// which means the user cannot refresh the JWT Token. If the user has multiple logins,
    /// then all the users logins will marked as a invalid. 
    /// </summary>
    /// <param name="userId"></param>
    Task LogoutUserViaUserIdAsync(string userId);
}