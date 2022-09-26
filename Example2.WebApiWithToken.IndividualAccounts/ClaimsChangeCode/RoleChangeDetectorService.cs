// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Net.DistributedFileStoreCache;

namespace Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;

/// <summary>
/// This detects changes in the AuthP's Roles and then 
/// This service will be added to the AuthPermissionsDbContext
/// </summary>
public class RoleChangeDetectorService : IDatabaseStateChangeEvent
{

    private readonly IDistributedFileStoreCacheClass _fsCache;
    private readonly AuthPermissionsOptions _options;
    private readonly ILogger<RoleChangeDetectorService> _logger;

    public RoleChangeDetectorService(IDistributedFileStoreCacheClass fsCache, 
        AuthPermissionsOptions options, ILogger<RoleChangeDetectorService> logger = null)
    {
        _fsCache = fsCache;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// This will register a method to the EF Core StateChanged event.
    /// </summary>
    /// <param name="context"></param>
    public void RegisterEventHandlers(AuthPermissionsDbContext context)
    {
        //This handles changes
        void RegisterRoleChangeUpdateUsers(object sender, EntityStateChangedEventArgs e)
        {
            if ((e.Entry.Entity is RoleToPermissions || e.Entry.Entity is UserToRole)
                && e.NewState == EntityState.Modified
               )
            {
                //Either a RoleToPermissions or a UserToRole has changed

                var roleName = e.Entry.Entity is RoleToPermissions roleToPermissions
                    ? roleToPermissions.RoleName
                    : ((UserToRole)e.Entry.Entity).RoleName;
                UpdateAllUsersPermissionClaim(context, roleName);
            }
        }

        //This handles the creation of a new UserToRole
        void RegisterUserToRoleAddUpdateUsers(object sender, EntityTrackedEventArgs e)
        {
            if (e.Entry.Entity is UserToRole userToRole && e.FromQuery == false 
                                             && (e.Entry.State == EntityState.Added || e.Entry.State == EntityState.Deleted))
            {
                var permissionValue = CalcPermissionsForUser(context, userToRole.UserId) ?? "";
                _fsCache.Set(userToRole.UserId.FormReplacementPermissionsKey(), permissionValue);
                _logger?.LogInformation("UserId {0} had a new Role, which makes the permission values to {1}",
                    userToRole.UserId, string.Join(", ", permissionValue.Select(x => (int)x)));
            }
        }

        context.ChangeTracker.StateChanged += RegisterRoleChangeUpdateUsers;
        context.ChangeTracker.Tracked += RegisterUserToRoleAddUpdateUsers;
    }

    private void UpdateAllUsersPermissionClaim(AuthPermissionsDbContext context, string roleName)
    {
        foreach (var authUser in context.AuthUsers
                     .Where(x => x.UserRoles.Any(y => y.RoleName == roleName)))
        {
            //If not claims, then use empty string
            var permissionValue = CalcPermissionsForUser(context, authUser.UserId) ?? "";
            _fsCache.Set(authUser.UserId.FormReplacementPermissionsKey(), permissionValue);
            _logger?.LogInformation("User {0} has been updated to permission values {1}", 
                authUser.Email, string.Join(", ", permissionValue.Select(x => (int)x)));
        }
    }


    /// <summary>
    /// This code is taken from the <see cref="ClaimsCalculator"/> and changed to sync.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    private string CalcPermissionsForUser(AuthPermissionsDbContext context, string userId)
    {
        //This gets all the permissions, with a distinct to remove duplicates
        var permissionsForAllRoles = context.UserToRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.Role.PackedPermissionsInRole)
            .ToList();

        if (_options.TenantType.IsMultiTenant())
        {
            //We need to add any RoleTypes.TenantAdminAdd for a tenant user

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