// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using ExamplesCommonCode.IdentityCookieCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NetCore.AutoRegisterDi;

namespace Example4.ShopCode.RefreshUsersClaims;

[RegisterAsScoped]
public class DetectTenantDataKeyChangeService : IDetectTenantDataKeyChangeService
{
    private readonly IGlobalChangeTimeService _globalAccessor;
    public DetectTenantDataKeyChangeService(AuthPermissionsDbContext context, IGlobalChangeTimeService globalAccessor)
    {
        _globalAccessor = globalAccessor;
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