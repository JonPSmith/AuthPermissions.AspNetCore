// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using Example4.MvcWebApp.IndividualAccounts.Controllers;

namespace Example4.MvcWebApp.IndividualAccounts.Middleware;

public static class DownForMaintenanceConstants
{
    //Cache key constants

    public const string DownForMaintenancePrefix = "DownForMaintenance-";
    public static readonly string DownForMaintenanceAllAppDown = $"{DownForMaintenancePrefix}AllAppDown";


    //Redirect constants

    public static readonly string MaintenanceAllAppDownRedirect = $"/{MaintenanceControllerName}/{nameof(MaintenanceController.ShowAllDownStatus)}";

    //Various controller, actions, areas used to allow users to access these while in a down state
    public const string MaintenanceControllerName = "Maintenance";
    public const string AccountArea = "Identity";
}