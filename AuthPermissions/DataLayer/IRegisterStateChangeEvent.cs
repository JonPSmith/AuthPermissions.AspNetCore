// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace AuthPermissions.DataLayer;

/// <summary>
/// This is an optional service for the <see cref="AuthPermissionsDbContext"/> which
/// allows you register event handlers 
/// </summary>
public interface IRegisterStateChangeEvent
{
    /// <summary>
    /// This is called within the <see cref="AuthPermissionsDbContext"/> constructor.
    /// It allows you to register the events you need.
    /// </summary>
    void RegisterEventHandlers(AuthPermissionsDbContext context);
}