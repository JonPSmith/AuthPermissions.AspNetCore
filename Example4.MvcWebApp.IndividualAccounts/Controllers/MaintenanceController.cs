// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.DownStatusCode;
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
            .Where(x => x.Key.StartsWith(AppStatusExtensions.DownForStatusPrefix))
            .Select(x => new KeyValuePair<string,string>(x.Key, x.Value))
            .ToList();

        return View(downCacheList);
    }

    [HasPermission(Example4Permissions.MaintenanceAllDown)]
    public IActionResult TakeAllDown()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example4Permissions.MaintenanceAllDown)]
    public IActionResult TakeAllDown(AllAppDownDto data)
    {
        data.UserId = User.GetUserIdFromUser();
        data.StartedUtc = DateTime.UtcNow;

        _fsCache.SetClass(AppStatusExtensions.DownForStatusAllAppDown, data);
        return RedirectToAction("Index", new { });
    }

    public IActionResult Remove(string key)
    {
        _fsCache.Remove(key);
        return RedirectToAction("Index", new { });
    }

    //---------------------------------------------------------------------

    public IActionResult ShowAllDownStatus()
    {
        var dto = _fsCache.GetClass<AllAppDownDto>(AppStatusExtensions.DownForStatusAllAppDown);
        return View(dto);
    }

    public IActionResult ShowTenantDownStatus()
    {
        return View();
    }

    public IActionResult ShowTenantDeleted()
    {
        return View();
    }
}