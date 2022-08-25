// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Example6.MvcWebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example6.MvcWebApp.Sharding.Controllers;

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

    [HasPermission(Example6Permissions.AppStatusAllDown)]
    public IActionResult TakeAllDown()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example6Permissions.AppStatusAllDown)]
    public IActionResult TakeAllDown(ManuelAppDownDto data)
    {
        data.UserId = User.GetUserIdFromUser();
        data.StartedUtc = DateTime.UtcNow;

        _downService.SetAppDown(data);
        return RedirectToAction("Index", new { });
    }

    [HasPermission(Example6Permissions.AppStatusTenantDown)]
    public async Task<IActionResult> TakeTenantDown([FromServices] IAuthTenantAdminService tenantAdminService)
    {
        return View(await ManuelTenantDownDto.SetupListOfTenantsAsync(tenantAdminService));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example6Permissions.AppStatusTenantDown)]
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
    [HasPermission(Example6Permissions.AppStatusRemove)]
    public IActionResult Remove(string key)
    {
        _downService.RemoveAnyDown(key);
        return RedirectToAction("Index", new { });
    }

    //---------------------------------------------------------------------
    //divert pages to tell the user why they are diverted

    public IActionResult ShowAppDownStatus()
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