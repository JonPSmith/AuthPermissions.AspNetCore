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
    /// This is the prefix on all the app and tenant "down" keys
    /// </summary>
    public const string DownForStatusPrefix = "AppStatus-";
    /// <summary>
    /// This is the key for the "app down" entry
    /// </summary>
    public static readonly string DownForStatusAllAppDown = $"{DownForStatusPrefix}AllAppDown";
    /// <summary>
    /// This is the prefix on all the tenant "down" keys
    /// </summary>
    public static readonly string StatusTenantPrefix = $"{DownForStatusPrefix}Tenant";
    /// <summary>
    /// This is the prefix on tenant "down for update" keys (temporary, while change)
    /// </summary>
    public static readonly string DownForStatusTenantUpdate = $"{StatusTenantPrefix}{nameof(TenantDownVersions.Update)}-";
    /// <summary>
    /// This is the prefix on tenant "manual down" keys (controlled by admin user)
    /// </summary>
    public static readonly string DownForStatusTenantManuel = $"{StatusTenantPrefix}{nameof(TenantDownVersions.ManualDown)}-";
    /// <summary>
    /// This is the prefix on tenants that have been deleted (permanent)
    /// </summary>
    public static readonly string DeletedTenantStatus = $"{StatusTenantPrefix}{nameof(TenantDownVersions.Deleted)}-";

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
        if (user.Identity?.IsAuthenticated != true)
        {
            //Let non-logged in user to access any 
            await next();
            return;
        }

        var controllerName = (string)_routeData.Values["controller"];
        var action = (string)_routeData.Values["action"];
        var area = (string)_routeData.Values["area"];
        if (controllerName == _statusControllerName || area == AccountArea)
        {
            // This allows the Status controller to show the banner and users to log in/out
            // The log in/out is there because if the user that set up the Status status logged out they wouldn't be able to log in again! 
            await next();
            return;
        }

        var fsCache = _serviceProvider.GetRequiredService<IDistributedFileStoreCacheClass>();
        var downCacheList = fsCache.GetAllKeyValues()
            .Where(x => x.Key.StartsWith(DownForStatusPrefix))
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value))
            .ToList();

        var allDownData = fsCache.GetClassFromString<ManuelAppDownDto>(
            downCacheList.SingleOrDefault(x => x.Key == DownForStatusAllAppDown).Value);
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

        //This will select both the TenantDown (for edit, move and manuel) and TenantDeleted 
        var userDataKey = user.GetAuthDataKeyFromUser();
        if (userDataKey != null)
        {
            var userTnCombinedKey = _tenantTypes.FormUniqueTenantValue(user);

            var tenantStatues = downCacheList
                .Where(x => x.Key.StartsWith(StatusTenantPrefix))
                .ToList();
            if (tenantStatues.Any())
            {
                //the current user is linked to a tenant and there are at least one tenant that shouldn't be accessed
                //Therefore we need to compare all the tenantDowns' Value, which contains the tenant's DataKey, with the user's DataKey

                //because we are in a hierarchical multi-tenant app we check user's DataKey starts with the downed tenant datakey
                var foundEntry = _tenantTypes.HasFlag(TenantTypes.HierarchicalTenant)
                    ? tenantStatues.FirstOrDefault(x => x.Value.StartsWith(userTnCombinedKey))
                    : tenantStatues.FirstOrDefault(x => x.Value == userTnCombinedKey);
                if (!foundEntry.Equals(new KeyValuePair<string, string>()))
                {
                    if (foundEntry.Key.StartsWith(DownForStatusTenantUpdate))
                        //This user isn't allowed to access the tenant while the change is made
                        redirect(StatusTenantDownRedirect);
                    else if (foundEntry.Key.StartsWith(DownForStatusTenantManuel))
                        //This tenant is deleted, so the user is always redirected
                        redirect(StatusTenantManualDownRedirect);
                    else //This tenant is deleted, so the user is always redirected
                        redirect(StatusTenantDeletedRedirect);


                    return;
                }
            }
        }

        //If it gets to here, then the user is allowed to access the application and its databases
        await next();

    }
}