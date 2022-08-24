// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.DownStatusCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers;

//Stop non-logged in user getting to StatusController
[Authorize]
public class StatusController : Controller
{
    private readonly ISetRemoveStatusService _downService;

    public StatusController(ISetRemoveStatusService downService)
    {
        _downService = downService;
    }

    public IActionResult Index(string message)
    {
        ViewBag.Message = message;

        var downCacheList = _downService.GetAllDownKeyValues();

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

        _downService.SetAppDown(data);
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
        await _downService.SetTenantDownWithDelayAsync(TenantDownVersions.ManualDown, data.TenantId);
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
        _downService.RemoveAnyDown(key);
        return RedirectToAction("Index", new { });
    }

    //---------------------------------------------------------------------

    public IActionResult ShowAllDownStatus()
    {
        return View(_downService.GetAppDownMessage());
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