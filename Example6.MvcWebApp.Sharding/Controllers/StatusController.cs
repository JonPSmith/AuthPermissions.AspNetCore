// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using Example6.MvcWebApp.Sharding.PermissionsCode;
using ExamplesCommonCode.DownStatusCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.DistributedFileStoreCache;

namespace Example6.MvcWebApp.Sharding.Controllers;

//Stop non-logged in user getting to StatusController
[Authorize]
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
            .Where(x => x.Key.StartsWith(RedirectUsersViaStatusData.DownForStatusPrefix))
            .Select(x => new KeyValuePair<string,string>(x.Key, x.Value))
            .ToList();

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

        _fsCache.SetClass(RedirectUsersViaStatusData.DownForStatusAllAppDown, data);
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
        await _fsCache.AddManualTenantDownStatusCacheAndWaitAsync(data.DataKey);
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
        _fsCache.Remove(key);
        return RedirectToAction("Index", new { });
    }

    //---------------------------------------------------------------------

    public IActionResult ShowAllDownStatus()
    {
        var dto = _fsCache.GetClass<ManuelAppDownDto>(RedirectUsersViaStatusData.DownForStatusAllAppDown);
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