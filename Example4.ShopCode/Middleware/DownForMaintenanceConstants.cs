// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace Example4.ShopCode.Middleware;

public static class DownForMaintenanceConstants
{
    public const string DownForMaintenancePrefix = "DownForMaintenance-";

    public static readonly string DownForMaintenanceAllAppDown = $"{DownForMaintenancePrefix}AllAppDown";


    //MaintenanceController names (can't use nameof because project isn't linked to the ASP.NET Core project)

    public const string MaintenanceControllerName = "Maintenance";
    public static readonly string MaintenanceAllAppDownRedirect = $"/{MaintenanceControllerName}/AllUsersDown";
    public const string AccountArea = "Identity";
}