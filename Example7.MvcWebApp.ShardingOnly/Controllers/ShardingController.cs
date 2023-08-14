using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.ShardingServices;
using Example7.MvcWebApp.ShardingOnly.Models;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using Microsoft.AspNetCore.Mvc;

namespace Example7.MvcWebApp.ShardingOnly.Controllers;

public class ShardingController : Controller
{
    private readonly IGetSetShardingEntries _shardingService;

    public ShardingController(IGetSetShardingEntries shardingService)
    {
        _shardingService = shardingService;
    }

    [HasPermission(Example7Permissions.ListDatabaseInfos)]
    public IActionResult Index(string message)
    {
        ViewBag.Message = message;

        return View(_shardingService.GetAllShardingEntries());
    }

    [HasPermission(Example7Permissions.TenantCreate)]
    public IActionResult Create()
    {
        var dto = new ShardingEntryEdit
        {
            AllPossibleConnectionNames = _shardingService.GetConnectionStringNames(),
            PossibleDatabaseTypes = _shardingService.PossibleDatabaseProviders
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example7Permissions.TenantCreate)]
    public IActionResult Create(ShardingEntryEdit data)
    {
        var status = _shardingService.AddNewShardingEntry(data.DatabaseInfo);

        if (status.HasErrors)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = status.GetAllErrors() });

        return RedirectToAction(nameof(Index), new { message = status.Message });
    }

    [HasPermission(Example7Permissions.UpdateDatabaseInfo)]
    public ActionResult Edit(string name)
    {
        var dto = new ShardingEntryEdit
        {
            DatabaseInfo = _shardingService.GetSingleShardingEntry(name),
            AllPossibleConnectionNames = _shardingService.GetConnectionStringNames(),
            PossibleDatabaseTypes = _shardingService.PossibleDatabaseProviders
        };

        if (dto.DatabaseInfo == null)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = $"Could not find a sharding entry with the name {name}." });

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example7Permissions.AddDatabaseInfo)]
    public ActionResult Edit(ShardingEntryEdit data)
    {
        var status = _shardingService.UpdateShardingEntry(data.DatabaseInfo);

        if (status.HasErrors)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = status.GetAllErrors() });

        return RedirectToAction(nameof(Index), new { message = status.Message });
    }

    [HasPermission(Example7Permissions.RemoveDatabaseInfo)]
    public IActionResult Remove(string name)
    {
        if (_shardingService.GetSingleShardingEntry(name) == null)
            return RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = $"Could not find the sharding entry with the name of '{name}'." });

        return View((object)name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(Example7Permissions.RemoveDatabaseInfo)]
    public IActionResult Remove(string nameToRemove, bool dummyValue)
    {
        var status = _shardingService.RemoveShardingEntry(nameToRemove);

        return status.HasErrors
            ? RedirectToAction(nameof(ErrorDisplay),
                new { errorMessage = status.GetAllErrors() })
            : RedirectToAction(nameof(Index), new { message = status.Message });
    }


    public ActionResult ErrorDisplay(string errorMessage)
    {
        return View((object)errorMessage);
    }
}