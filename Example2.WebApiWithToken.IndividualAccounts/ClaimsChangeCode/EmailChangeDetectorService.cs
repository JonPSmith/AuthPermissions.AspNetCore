// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Net.DistributedFileStoreCache;

namespace Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;


public class EmailChangeDetectorService : IDatabaseStateChangeEvent
{

    private readonly IDistributedFileStoreCacheClass _fsCache;

    public EmailChangeDetectorService(IDistributedFileStoreCacheClass fsCache)
    {
        _fsCache = fsCache;
    }

    /// <summary>
    /// This will register a event method to detect that the user's Email has changed.
    /// If the Email has changed, then the FileStore cache value will be removed, which
    /// causes the <see cref="AddEmailClaimMiddleware"/> code to read the user's new email again
    /// </summary>
    /// <param name="context"></param>
    public void RegisterEventHandlers(AuthPermissionsDbContext context)
    {
        context.ChangeTracker.StateChanged += delegate(object sender, EntityStateChangedEventArgs e)
        {
            if (e.Entry.Entity is AuthUser user
                && e.NewState == EntityState.Modified
                && e.Entry.OriginalValues[nameof(AuthUser.Email)] != e.Entry.CurrentValues[nameof(AuthUser.Email)]
                )
                //Email has changed, so we remove the current cache value, which causes the email to be reevaluated 
                _fsCache.Remove(user.UserId.FormAddedEmailClaimKey());
        };
    }

}