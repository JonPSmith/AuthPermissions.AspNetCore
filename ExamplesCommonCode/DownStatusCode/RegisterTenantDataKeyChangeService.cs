// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ExamplesCommonCode.DownStatusCode;

public class RegisterTenantKeyOrShardChangeService : IRegisterStateChangeEvent
{
    private readonly IGlobalChangeTimeService _globalAccessor;
    public RegisterTenantKeyOrShardChangeService(IGlobalChangeTimeService globalAccessor)
    {
        _globalAccessor = globalAccessor;
    }

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