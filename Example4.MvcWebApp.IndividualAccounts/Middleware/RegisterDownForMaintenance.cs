// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.CommonCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Net.DistributedFileStoreCache;

namespace Example4.MvcWebApp.IndividualAccounts.Middleware;

public static class RegisterDownForMaintenance
{
    public static void AddDownForMaintenance(this WebApplication app)
    {
        app.Use(async (HttpContext context, Func<Task> next) =>
        {
            var all = context.GetRouteData();
            var controllerName = (string)context.GetRouteData().Values["controller"];
            var action = (string)context.GetRouteData().Values["action"];
            var area = (string)context.GetRouteData().Values["area"];
            if (controllerName == DownForMaintenanceConstants.MaintenanceControllerName
                || area == DownForMaintenanceConstants.AccountArea)
            {
                // This allows the Maintenance controller to show the banner and users to log in/out
                // The log in/out is there because if the user that set up the maintenance status logged out they wouldn't be able to log in again! 
                await next();
                return;
            }

            var fsCache = context.RequestServices.GetRequiredService<IDistributedFileStoreCacheClass>();
            var allDownData = fsCache.GetClass<AllAppDownDto>(DownForMaintenanceConstants.DownForMaintenanceAllAppDown);
            if (allDownData == null)
                //If the AllAppDown isn't active, then anyone can access the app 
                await next();
            else
            {
                //There is a "Down For Maintenance" in effect, so only the person that set up this state can still access the app

                var userId = context.User.GetUserIdFromUser();
                if (userId != allDownData.UserId)
                    //The user isn't allowed to access the application 
                    context.Response.Redirect(DownForMaintenanceConstants.MaintenanceAllAppDownRedirect);
                else
                    //The user is the one that set up the "Down For Maintenance" status, so they have access to the app
                    await next();
            }
        });
    }
}