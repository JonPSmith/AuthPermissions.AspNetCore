using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using Example3.MvcWebApp.IndividualAccounts.Models;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Example3.MvcWebApp.IndividualAccounts.Controllers
{
    public class TenantController : Controller
    {
        private readonly IAuthTenantAdminService _authTenantAdmin;

        public TenantController(IAuthTenantAdminService authTenantAdmin)
        {
            _authTenantAdmin = authTenantAdmin;
        }

        [HasPermission(Example3Permissions.TenantList)]
        public async Task<IActionResult> Index(string message)
        {
            var tenantNames = await TenantDto.TurnIntoDisplayFormat( _authTenantAdmin.QueryTenants())
                .OrderBy(x => x.TenantName)
                .ToListAsync();

            ViewBag.Message = message;

            return View(tenantNames);
        }

        [HasPermission(Example3Permissions.TenantCreate)]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example3Permissions.TenantCreate)]
        public async Task<IActionResult> Create(TenantDto input)
        {
            var status = await _authTenantAdmin.AddSingleTenantAsync(input.TenantName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(Example3Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new TenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example3Permissions.TenantUpdate)]
        public async Task<IActionResult> Edit(TenantDto input)
        {
            var status = await _authTenantAdmin
                .UpdateTenantNameAsync(input.TenantId, input.TenantName);

            return status.HasErrors
                ? RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() })
                : RedirectToAction(nameof(Index), new { message = status.Message });
        }


        [HasPermission(Example3Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(int id)
        {
            var status = await _authTenantAdmin.GetTenantViaIdAsync(id);
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return View(new TenantDto
            {
                TenantId = id,
                TenantName = status.Result.TenantFullName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Example3Permissions.TenantDelete)]
        public async Task<IActionResult> Delete(TenantDto input)
        {
            var status = await _authTenantAdmin.DeleteTenantAsync(input.TenantId);

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
