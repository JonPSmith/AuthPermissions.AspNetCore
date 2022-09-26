// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// Use this to register the "down for maintenance" middleware.
/// This MUST be registered AFTER the UseAuthorization middleware
/// </summary>
public static class RegisterDownForMaintenance
{
    /// <summary>
    /// Register the "down for maintenance" middleware
    /// </summary>
    /// <param name="app"></param>
    /// <param name="tenantTypes">Provides the <see cref="TenantTypes"/> for the application</param>
    public static void UseDownForMaintenance(this IApplicationBuilder app, TenantTypes tenantTypes)
    {
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            var handler = new RedirectUsersViaStatusData(context.GetRouteData(), context.RequestServices, tenantTypes);

            await handler.RedirectUserOnStatusesAsync(context.User, context.Response.Redirect, next);
        });
    }
}