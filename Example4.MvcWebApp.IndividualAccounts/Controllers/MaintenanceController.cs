// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example4.ShopCode.Middleware;
using Microsoft.AspNetCore.Mvc;
using Net.DistributedFileStoreCache;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers;

public class MaintenanceController : Controller
{
    private readonly IDistributedFileStoreCacheClass _fsCache;

    public MaintenanceController(IDistributedFileStoreCacheClass fsCache)
    {
        _fsCache = fsCache;
    }


    public IActionResult Index(string message)
    {
        ViewBag.Message = message;

        var downCacheList = _fsCache.GetAllKeyValues()
            .Where(x => x.Key.StartsWith(DownForMaintenanceConstants.DownForMaintenancePrefix))
            .Select(x => new KeyValuePair<string,string>(x.Key, x.Value))
            .ToList();

        return View(downCacheList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example4Permissions.MaintenanceAllDown)]
    public IActionResult TakeAllDown()
    {
        _fsCache.Set(DownForMaintenanceConstants.DownForMaintenanceAllAppDown, User.GetUserIdFromUser());
        return RedirectToAction("Index", new { });
    }

    public IActionResult Remove(string key)
    {
        _fsCache.Remove(key);
        return RedirectToAction("Index", new { });
    }

    //---------------------------------------------------------------------

    public IActionResult AllUsersDown()
    {
        return View((object)"The site is down for maintenance. Please check back later.");
    }
}