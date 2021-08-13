using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AuthPermissions.AdminCode;
using Example4.MvcWebApp.IndividualAccounts.Models;
using Microsoft.EntityFrameworkCore;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class RolesController : Controller
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public RolesController(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        //[HasPermission(Example4Permissions.RoleRead)]
        public async Task<IActionResult> Index(string message)
        {
            var permissionDisplay = await
                _authRolesAdmin.QueryRoleToPermissions().ToListAsync();

            ViewBag.Message = message;

            return View(permissionDisplay);
        }

        //[HasPermission(Example4Permissions.PermissionRead)]
        public IActionResult ListPermissions()
        {
            var permissionDisplay = _authRolesAdmin.GetPermissionDisplay(false);

            return View(permissionDisplay);
        }

        //[HasPermission(Example4Permissions.RoleChange)]
        public async Task<IActionResult> Edit(string roleName)
        {
            var role = await
                _authRolesAdmin.QueryRoleToPermissions().SingleOrDefaultAsync(x => x.RoleName == roleName);
            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            return View(role == null ? null : new RoleAddUpdateDisplayDto(role.RoleName, role.Description, role.PermissionNames, permissionsDisplay));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleAddUpdateInputDto input)
        {
            var status = await _authRolesAdmin
                .UpdateRoleToPermissionsAsync(input.RoleName, input.Permissions, input.Description);

            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public IActionResult Create()
        {
            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            return View(new RoleAddUpdateDisplayDto(null, null, null, permissionsDisplay));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleAddUpdateInputDto input)
        {
            var status = await _authRolesAdmin
                .CreateRoleToPermissionsAsync(input.RoleName, input.Permissions, input.Description);

            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }

        public async Task<IActionResult> Delete(string roleName)
        {

            return View(await RoleDeleteConfirmDto.FormRoleDeleteConfirmDtoAsync(roleName, _authRolesAdmin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(RoleDeleteConfirmDto input)
        {
            var status = await _authRolesAdmin.DeleteRoleAsync(input.RoleName, input.ConfirmDelete.Trim() == input.RoleName);
                
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }
    }
}
