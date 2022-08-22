// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.DownStatusCode;
using Microsoft.AspNetCore.Mvc;
using Net.DistributedFileStoreCache;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers;

public class StatusController : Controller
{
    private readonly IDistributedFileStoreCacheClass _fsCache;

    public StatusController(IDistributedFileStoreCacheClass fsCache)
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

    [HasPermission(Example4Permissions.AppStatusAllDown)]
    public IActionResult TakeAllDown()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example4Permissions.AppStatusAllDown)]
    public IActionResult TakeAllDown(ManuelAppDownDto data)
    {
        data.UserId = User.GetUserIdFromUser();
        data.StartedUtc = DateTime.UtcNow;

        _fsCache.SetClass(AppStatusExtensions.DownForStatusAllAppDown, data);
        return RedirectToAction("Index", new { });
    }


    [HasPermission(Example4Permissions.AppStatusTenantDown)]
    public async Task<IActionResult> TakeTenantDown([FromServices] IAuthTenantAdminService tenantAdminService)
    {
        return View(await ManuelTenantDownDto.SetupListOfTenantsAsync(tenantAdminService));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example4Permissions.AppStatusTenantDown)]
    public async Task<IActionResult> TakeTenantDown(ManuelTenantDownDto data)
    {
        await _fsCache.AddManualTenantDownStatusCacheAndWaitAsync(data.DataKey);
        return RedirectToAction("Index", new { });
    }

    /// <summary>
    /// This can remove ANY down status from the list
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [HasPermission(Example4Permissions.AppStatusRemove)]
    public IActionResult Remove(string key)
    {
        _fsCache.Remove(key);
        return RedirectToAction("Index", new { });
    }

    //---------------------------------------------------------------------

    public IActionResult ShowAllDownStatus()
    {
        var dto = _fsCache.GetClass<ManuelAppDownDto>(AppStatusExtensions.DownForStatusAllAppDown);
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

    public IActionResult ShowTenantManuallyDown()
    {
        return View();
    }
}