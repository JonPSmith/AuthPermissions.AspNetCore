// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This handles the redirection of user based on the down statues held in the FileStore cache.
/// Putting this in a separate class makes it easier to test
/// </summary>
public class RedirectUsersViaStatusData
{
    //Cache key constants
    /// <summary>
    /// This is the prefix on all diverts of users
    /// </summary>
    public const string DownForStatusPrefix = "Divert";
    /// <summary>
    /// This is the key for the "app down" entry
    /// </summary>
    public static readonly string DivertAppDown = $"{DownForStatusPrefix}AppDown";
    /// <summary>
    /// This is the prefix on all the tenant "down" keys
    /// </summary>
    public static readonly string DivertTenantPrefix = $"{DownForStatusPrefix}Tenant";
    /// <summary>
    /// This is the prefix on tenant "down for update" keys (temporary, while change)
    /// </summary>
    public static readonly string DivertTenantUpdate = $"{DivertTenantPrefix}{nameof(TenantDownVersions.Update)}-";
    /// <summary>
    /// This is the prefix on tenant "manual down" keys (controlled by admin user)
    /// </summary>
    public static readonly string DivertTenantManuel = $"{DivertTenantPrefix}{nameof(TenantDownVersions.ManualDown)}-";
    /// <summary>
    /// This is the prefix on tenants that have been deleted (permanent)
    /// </summary>
    public static readonly string DivertTenantDeleted = $"{DivertTenantPrefix}{nameof(TenantDownVersions.Deleted)}-";

    private string StatusAllAppDownRedirect => $"/{_statusControllerName}/ShowAppDownStatus";
    private string StatusTenantDownRedirect => $"/{_statusControllerName}/ShowTenantDownStatus";
    private string StatusTenantDeletedRedirect => $"/{_statusControllerName}/ShowTenantDeleted";
    private string StatusTenantManualDownRedirect => $"/{_statusControllerName}/ShowTenantManuallyDown";

    //Various controller, actions, areas used to allow users to access these while in a down state
    private const string AccountArea = "Identity";

    private readonly RouteData _routeData;
    private readonly IServiceProvider _serviceProvider;
    private readonly TenantTypes _tenantTypes;
    private readonly string _statusControllerName;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="routeData">This should contain the HttpContext's GetRouteData()</param>
    /// <param name="serviceProvider">The service provider from the HttpContext</param>
    /// <param name="tenantTypes">Defines what </param>
    /// <param name="statusControllerName">This defines the name of the controller where the status </param>
    public RedirectUsersViaStatusData(RouteData routeData, IServiceProvider serviceProvider,
        TenantTypes tenantTypes, string statusControllerName = "Status")
    {
        _routeData = routeData;
        _serviceProvider = serviceProvider;
        _tenantTypes = tenantTypes;
        _statusControllerName = statusControllerName;
    }

    /// <summary>
    /// This checks if there are any "down" status entries in the FileStore cache, and if the user meets a "down" status.
    /// The "app down" effects everybody but the user that took the app down, while the "tenant downs" only effects users
    /// that have the same tenant key and the downed tenant
    /// </summary>
    /// <param name="user">The user, after the authorization has created it, with its claims</param>
    /// <param name="redirect">This allows a user to be diverted to "down" message</param>
    /// <param name="next">You call this if the user is allowed to go to the url they asked for</param>
    /// <returns></returns>
    public async Task RedirectUserOnStatusesAsync(ClaimsPrincipal user, Action<string> redirect, Func<Task> next)
    {
        //STAGE 1: certain urls always let though, e.g. login/logout
        var controllerName = (string)_routeData.Values["controller"];
        //var action = (string)_routeData.Values["action"];
        var area = (string)_routeData.Values["area"];
        if (controllerName == _statusControllerName || area == AccountArea)
        {
            // Access Status controller, allowing admin to stop a divert.
            // Access log in / log out. Need that in case admin logs out while AppDown divert is on.
            await next();
            return;
        }

        var fsCache = _serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();

        //STAGE 2: If AppDown, then only allow the admin user who “downed” the App
        var allDownData = fsCache.GetClass<ManuelAppDownDto>(DivertAppDown);
        if (allDownData != null)
        {
            //There is a "Down For Status" in effect, so only the person that set up this state can still access the app

            var userId = user.GetUserIdFromUser();
            if (userId != allDownData.UserId)
            {
                //The user isn't allowed to access the application 
                redirect(StatusAllAppDownRedirect);
                return;
            }
        }

        //STAGE 3: If user is linked to a “down” tenant, then divert to the correct url
        //This will select both the TenantDown (for edit, move and manuel) and TenantDeleted 
        var userDataKey = user.GetAuthDataKeyFromUser();
        if (userDataKey != null)
        {
            var userTnCombinedKey = _tenantTypes.FormUniqueTenantValue(user);
            var tenantCacheKey = fsCache.GetAllKeyValues()
                    .Where(x =>
                        //The key check depends on the whether the tenant is a hierarchical or nor
                        (_tenantTypes.HasFlag(TenantTypes.HierarchicalTenant)
                            ? x.Value.StartsWith(userTnCombinedKey)
                            : x.Value == userTnCombinedKey)
                        && x.Key.StartsWith(DivertTenantPrefix))
                    .Select(x => x.Key)
                    .FirstOrDefault();
            if (tenantCacheKey != null)
            {
                //the current user is linked to a tenant that has a divert

                if (tenantCacheKey.StartsWith(DivertTenantUpdate))
                    //This user isn't allowed to access the tenant while the change is made
                    redirect(StatusTenantDownRedirect);
                else if (tenantCacheKey.StartsWith(DivertTenantManuel))
                    //This tenant is deleted, so the user is always redirected
                    redirect(StatusTenantManualDownRedirect);
                else //This tenant is deleted, so the user is always redirected
                    redirect(StatusTenantDeletedRedirect);


                return;
            }
        }

        //If it gets to here, then the user is allowed to access the application and its databases
        await next();

    }
}