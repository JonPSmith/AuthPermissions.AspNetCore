// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.DataLayer;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using ExamplesCommonCode.IdentityCookieCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Example4.ShopCode.RefreshUsersClaims;

public class RegisterTenantDataKeyChangeService : IRegisterStateChangeEvent
{
    private readonly IGlobalChangeTimeService _globalAccessor;
    public RegisterTenantDataKeyChangeService(IGlobalChangeTimeService globalAccessor)
    {
        _globalAccessor = globalAccessor;
    }

    public void RegisterEventHandlers(AuthPermissionsDbContext context)
    {
        context.ChangeTracker.StateChanged += RegisterDataKeyChange;
    }

    private void RegisterDataKeyChange(object sender, EntityStateChangedEventArgs e)
    {
        if (e.Entry.Entity is Tenant
            && e.NewState == EntityState.Modified
            && e.Entry.OriginalValues[nameof(Tenant.ParentDataKey)] !=
            e.Entry.CurrentValues[nameof(Tenant.ParentDataKey)])
        {
            //A tenant DataKey has changed due to a Move, so we need to update the 
            _globalAccessor.SetGlobalChangeTimeToNowUtc();
        }
    }
}