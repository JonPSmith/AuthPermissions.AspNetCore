// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Example6.MvcWebApp.Sharding.Models;
using Example6.MvcWebApp.Sharding.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example6.MvcWebApp.Sharding.Controllers
{
    public class TenantController : Controller
    {
        private readonly IAuthTenantAdminService _authTenantAdmin;
        private readonly ISetRemoveStatus _status;

        public TenantController(IAuthTenantAdminService authTenantAdmin, ISetRemoveStatus status)
        {
            _authTenantAdmin = authTenantAdmin;
            _status = status;
        }

        [HasPermission(Example6Permissions.TenantList)]
        public async Task<IActionResult> Index(string message)
        {
            var tenantNames = await ShardingSingleLevelTenantDto.TurnIntoDisplayFormat( _authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantName)
                .ToListAsync();

            ViewBag.Message = message;

            return View(tenantNames);
        }

        [HasPermission(Example6Permissions.ListDbsWithTenants)]
        public async Task<IActionResult> ListDatabases([FromServices] IShardingConnections connect)
        {
            var connections = await connect.GetDatabaseInfoNamesWithTenantNamesAsync();

            return View(connections);
        }

        [HasPermission(Example6Permissions.TenantCreate)]
        public IActionResult Create([FromServices]AuthPermissionsOptions authOptions, 
        [FromServices]IShardingConnections connect)
        {
            return View(ShardingSingleLevelTenantDto.SetupForCreate(authOptions,
                connect.GetAllPossibleShardingData().Select(x => x.Name).ToList()
                ));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example6Permissions.TenantCreate)]
        public async Task<IActionResult> Create(ShardingSingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin.AddSingleTenantAsync(input.TenantName, null,
                input.HasOwnDb, input.ConnectionName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(Example6Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(int id)
        {
            return View(await ShardingSingleLevelTenantDto.SetupForUpdateAsync(_authTenantAdmin, id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example6Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(ShardingSingleLevelTenantDto input)
        {
            var removeDownAsync = await _status.SetTenantDownWithDelayAsync(TenantDownVersions.Update, input.TenantId);
            var status = await _authTenantAdmin
                .UpdateTenantNameAsync(input.TenantId, input.TenantName);
            await removeDownAsync();

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(Example6Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new ShardingSingleLevelTenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName,
                DataKey = status.Result.GetTenantDataKey()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example6Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(ShardingSingleLevelTenantDto input)
        {
            var status = await _authTenantAdmin.DeleteTenantAsync(input.TenantId);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(Example6Permissions.MoveTenantDatabase)]
        public async Task<IActionResult> MoveDatabase([FromServices] IShardingConnections connect, int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new ShardingSingleLevelTenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName,
                ConnectionName = status.Result.DatabaseInfoName,
                AllPossibleConnectionNames = connect.GetAllPossibleShardingData().Select(x => x.Name).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example6Permissions.MoveTenantDatabase)]
        public async Task<IActionResult> MoveDatabase(ShardingSingleLevelTenantDto input)
        {
            var removeDownAsync = await _status.SetTenantDownWithDelayAsync(TenantDownVersions.Update, input.TenantId);
            var status = await _authTenantAdmin.MoveToDifferentDatabaseAsync(
                input.TenantId, input.HasOwnDb, input.ConnectionName);
            await removeDownAsync();

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
}
