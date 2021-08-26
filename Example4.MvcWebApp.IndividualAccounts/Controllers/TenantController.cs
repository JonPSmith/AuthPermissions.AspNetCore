using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using Example4.MvcWebApp.IndividualAccounts.Models;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class TenantController : Controller
    {
        private readonly IAuthTenantAdminService _authTenantAdmin;

        public TenantController(IAuthTenantAdminService authTenantAdmin)
        {
            _authTenantAdmin = authTenantAdmin;
        }

        [HasPermission(Example4Permissions.TenantList)]
        public async Task<IActionResult> Index(string message)
        {
            var tenantNames = await TenantDto.TurnIntoDisplayFormat( _authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantFullName)
                .ToListAsync();

            ViewBag.Message = message;

            return View(tenantNames);
        }

        [HasPermission(Example4Permissions.TenantCreate)]
        public async Task<IActionResult> Create()
        {
            var model = await TenantDto.SetupForCreateAsync(_authTenantAdmin);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example4Permissions.TenantCreate)]
        public async Task<IActionResult> Create(TenantDto input)
        {
            var status = await _authTenantAdmin
                .AddHierarchicalTenantAsync(input.TenantName, input.ParentId);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(Example4Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(TenantDto.SetupForEdit(status.Result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example4Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(TenantDto input)
        {
            var status = await _authTenantAdmin
                .UpdateTenantNameAsync(input.TenantId, input.TenantName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(Example4Permissions.TenantMove)]
        public async Task<IActionResult> Move(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(await TenantDto.SetupForMoveAsync(status.Result, _authTenantAdmin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example4Permissions.TenantMove)]
        public async Task<IActionResult> Move(TenantDto input)
        {
            var status = await _authTenantAdmin
                .MoveHierarchicalTenantToAnotherParentAsync(input.TenantId, input.ParentId,
                    (tuple => { }));

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //ONLY FOR TEST
            await status.Result.SaveChangesAsync();

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message + " NOTE: DATE UPDATE NOT WRITTEN!!!!." });
        }

        [HasPermission(Example4Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(await TenantDto.SetupForDeleteAsync(status.Result, _authTenantAdmin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example4Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(TenantDto input)
        {
            var deleteInfo = new List<(string fullTenantName, string dataKey)>();
            var status = await _authTenantAdmin
                .DeleteTenantAsync(input.TenantId, (tuple => deleteInfo.Add(tuple)));

            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View("DeleteConfirm", new TenantDeleteInfo(status.Message, deleteInfo));
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }
    }
}
