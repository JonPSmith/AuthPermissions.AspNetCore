// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExamplesCommonCode.DownStatusCode;

public static class RegisterDownForMaintenance
{
    public static void AddDownForMaintenance(this WebApplication app, TenantTypes tenantTypes)
    {
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            var handler = new RedirectUsersViaStatusData(context.GetRouteData(), context.RequestServices, tenantTypes);

            await handler.RedirectUserOnStatusesAsync(context.User, context.Response.Redirect, next);
        });
    }
}