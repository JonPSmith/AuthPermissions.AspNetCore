// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.BaseCode.DataLayer.EfCode;

/// <summary>
/// This interface allows you to add a custom configuration via the <see cref="DbContext.OnModelCreating"/>
/// within the <see cref="AuthPermissionsDbContext"/>. This allows a developer to provide extra configurations
/// to the DbContext. This is useful when you are using a custom database type and you need to add concurrency
/// token to every entity. 
/// </summary>
public interface ICustomConfiguration
{
    /// <summary>
    /// This method will be called 
    /// </summary>
    /// <param name="modelBuilder"></param>
    public void ApplyCustomConfiguration(ModelBuilder modelBuilder);
}