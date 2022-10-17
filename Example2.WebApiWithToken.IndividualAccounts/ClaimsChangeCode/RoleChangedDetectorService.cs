// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Net.DistributedFileStoreCache;

namespace Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;

/// <summary>
/// This detects changes in the AuthP's Roles and then 
/// This service will be added to the AuthPermissionsDbContext
/// NOTE: because this is used with Web API with a JWT Token with refresh you could make the
/// cache value time out after the refresh time, as by then the claims will have been updated
/// </summary>
public class RoleChangedDetectorService : IDatabaseStateChangeEvent
{
    private readonly IDistributedFileStoreCacheClass _fsCache;
    private readonly AuthPermissionsOptions _options;
    private readonly ILogger<RoleChangedDetectorService> _logger;

    public RoleChangedDetectorService(IDistributedFileStoreCacheClass fsCache, 
        AuthPermissionsOptions options, ILogger<RoleChangedDetectorService> logger = null)
    {
        _fsCache = fsCache;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// This will register a method to the EF Core SavedChanges event to find the Users
    /// that need their PackedPermission's claim to be overrides with the changed database
    /// NOTE: This code does NOT handle Roles of RoleTypes.TenantAutoAdd. This could be added, but its not there now
    /// </summary>
    /// <param name="context"></param>
    public void RegisterEventHandlers(AuthPermissionsDbContext context)
    {
        var effectedUserIds = new List<string>();

        //This catches the changes before SaveChanges is called
        context.SavingChanges += delegate(object dbContext, SavingChangesEventArgs args)
        {
            var allTrackedEntities = ((DbContext)dbContext).ChangeTracker.Entries()
                .ToList();

            effectedUserIds = allTrackedEntities
                .Where(x => x.Entity is UserToRole && x.State != EntityState.Unchanged)
                .Select(x => ((UserToRole)x.Entity).UserId)
                .Distinct().ToList();

            //This looks at any modified RoleToPermissions (adds and deletes are handed by the UserToRoles)
            foreach (var entry in allTrackedEntities.Where(x => x.Entity is RoleToPermissions && x.State == EntityState.Modified))
            {
                effectedUserIds.AddRange(((AuthPermissionsDbContext)dbContext).UserToRoles
                    .Where(x => x.RoleName == ((RoleToPermissions)entry.Entity).RoleName)
                    .Select(x => x.UserId));
            }
        };

        //This removes the UserIds if the SaveChange fails
        context.SaveChangesFailed += (sender, args) =>
        {
            effectedUserIds = new List<string>();
        };

        //This is called if the SaveChanges was successful. At this point the database is in the correct 
        context.SavedChanges += delegate(object dbContext, SavedChangesEventArgs args) 
        {
            AddPermissionOverridesToCache((AuthPermissionsDbContext)dbContext, effectedUserIds.Distinct());
            effectedUserIds = new List<string>();
        };
    }

    private void AddPermissionOverridesToCache(AuthPermissionsDbContext context, IEnumerable<string> effectedUserIds)
    {
        var entriesToCache = new List<KeyValuePair<string,string>>();
        foreach (var userIdAndPackedPermission in context.AuthUsers
                     .Where(x => effectedUserIds.Contains(x.UserId))
                     .Select(x => new{ x.UserId, packedPermissions = 
                         x.UserRoles.Select(y => y.Role.PackedPermissionsInRole).ToList()})
                 )
        {
            //If not claims, then use empty string
            var permissionValue = CalcPermissionsForUser(context, userIdAndPackedPermission.UserId, userIdAndPackedPermission.packedPermissions) ?? "";
            entriesToCache.Add(new(userIdAndPackedPermission.UserId.FormReplacementPermissionsKey(), permissionValue));
            _logger?.LogInformation("UserId {0} has been updated to permission values {1}",
                userIdAndPackedPermission.UserId, string.Join(", ", permissionValue.Select(x => (int)x)));
        }
        if (entriesToCache.Any())
            //NOTE: You might like to add a expiration time on the cache entries if your users claims are regularly updated
            //e.g. if you are JWT Token with the refresh feature, then you could make the cache expire after the refresh time.
            _fsCache.SetMany(entriesToCache);
    }

    /// <summary>
    /// This takes the assigned <see cref="RoleToPermissions.PackedPermissionsInRole"/> for the user
    /// and then adds any <see cref="RoleTypes.TenantAutoAdd"/> Roles if provided to create the user's final PackedPermissions
    /// This code is taken from the <see cref="ClaimsCalculator"/> and changed
    /// </summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    /// <param name="permissionsForAllRoles"></param>
    /// <returns></returns>
    private string CalcPermissionsForUser(AuthPermissionsDbContext context, string userId, List<string> permissionsForAllRoles)
    {
        if (_options.TenantType.IsMultiTenant())
        {
            //We need to add any RoleTypes.TenantAutoAdd for a tenant user

            var autoAddPermissions = context.AuthUsers
                .Where(x => x.UserId == userId && x.TenantId != null)
                .SelectMany(x => x.UserTenant.TenantRoles
                    .Where(y => y.RoleType == RoleTypes.TenantAutoAdd)
                    .Select(z => z.PackedPermissionsInRole))
                .ToList();

            if (autoAddPermissions.Any())
                permissionsForAllRoles.AddRange(autoAddPermissions);
        }

        if (!permissionsForAllRoles.Any())
            return null;

        //thanks to https://stackoverflow.com/questions/5141863/how-to-get-distinct-characters
        var packedPermissionsForUser = new string(string.Concat(permissionsForAllRoles).Distinct().ToArray());

        return packedPermissionsForUser;
    }
}