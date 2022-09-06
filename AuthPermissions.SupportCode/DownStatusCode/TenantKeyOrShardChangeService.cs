// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This service that will be added to the AuthPermissionsDbContext
/// which will sets an entry in the FileStore cache containing the last time that
/// the DataKey or DatabaseInfoName where changed in the tenant
/// </summary>
public class TenantKeyOrShardChangeService : IDatabaseStateChangeEvent
{
    private readonly IGlobalChangeTimeService _globalAccessor;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="globalAccessor"></param>
    public TenantKeyOrShardChangeService(IGlobalChangeTimeService globalAccessor)
    {
        _globalAccessor = globalAccessor;
    }

    /// <summary>
    /// This will register a method to the EF Core StateChanged event.
    /// If the registered method detects a change to the DataKey or DatabaseInfoName,
    /// then it sets the global change setting to the time of the last change.
    /// </summary>
    /// <param name="context"></param>
    public void RegisterEventHandlers(AuthPermissionsDbContext context)
    {
        context.ChangeTracker.StateChanged += RegisterKeyOrShardChange;
    }

    private void RegisterKeyOrShardChange(object sender, EntityStateChangedEventArgs e)
    {
        if (e.Entry.Entity is Tenant
            && e.NewState == EntityState.Modified
            && (e.Entry.OriginalValues[nameof(Tenant.ParentDataKey)] != e.Entry.CurrentValues[nameof(Tenant.ParentDataKey)]
                || e.Entry.OriginalValues[nameof(Tenant.DatabaseInfoName)] != e.Entry.CurrentValues[nameof(Tenant.DatabaseInfoName)])
            )
        {
            //A tenant DataKey or the sharding DatabaseInfoName has changed due to a Move, so we need to update all the user's claims
            _globalAccessor.SetGlobalChangeTimeToNowUtc();
        }
    }
}