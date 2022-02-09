// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace AuthPermissions.DataLayer;

/// <summary>
/// This is an optional service for the <see cref="AuthPermissionsDbContext"/> which
/// allows you register event handlers 
/// </summary>
public interface IRegisterStateChangeEvent
{
    /// <summary>
    /// This returns a action to apply to the <see cref="AuthPermissionsDbContext"/>
    /// It is done this way because the DbContext mustn't be a copy
    /// </summary>
    void RegisterDataKeyChange(object sender, EntityStateChangedEventArgs e);
}