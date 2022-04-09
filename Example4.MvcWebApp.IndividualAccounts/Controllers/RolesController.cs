using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.CommonCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
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

        [HasPermission(Example4Permissions.RoleRead)]
        public async Task<IActionResult> Index(string message)
        {
            var userId = User.GetUserIdFromUser();
            var permissionDisplay = await
                _authRolesAdmin.QueryRoleToPermissions(userId).ToListAsync();

            ViewBag.Message = message;

            return View(permissionDisplay);
        }

        [HasPermission(Example4Permissions.PermissionRead)]
        public IActionResult ListPermissions()
        {
            var permissionDisplay = _authRolesAdmin.GetPermissionDisplay(false);

            return View(permissionDisplay);
        }

        [HasPermission(Example4Permissions.RoleChange)]
        public async Task<IActionResult> Edit(string roleName)
        {
            var userId = User.GetUserIdFromUser();
            var role = await
                _authRolesAdmin.QueryRoleToPermissions(userId).SingleOrDefaultAsync(x => x.RoleName == roleName);
            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            return View(role == null ? null : RoleCreateUpdateDto.SetupForCreateUpdate(role.RoleName, role.Description, role.PermissionNames, permissionsDisplay));
        }

        [HasPermission(Example4Permissions.RoleChange)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleCreateUpdateDto input)
        {
            var status = await _authRolesAdmin
                .UpdateRoleToPermissionsAsync(input.RoleName, input.GetSelectedPermissionNames(), input.Description, input.RoleType);

            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }

        [HasPermission(Example4Permissions.RoleChange)]
        public IActionResult Create()
        {
            var permissionsDisplay = _authRolesAdmin.GetPermissionDisplay(false);
            return View(RoleCreateUpdateDto.SetupForCreateUpdate(null, null, null, permissionsDisplay));
        }

        [HasPermission(Example4Permissions.RoleChange)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateUpdateDto input)
        {
            var status = await _authRolesAdmin
                .CreateRoleToPermissionsAsync(input.RoleName, input.GetSelectedPermissionNames(), input.Description);

            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }

        public ActionResult ErrorDisplay(string errorMessage)
        {
            return View((object)errorMessage);
        }

        [HasPermission(Example4Permissions.RoleChange)]
        public async Task<IActionResult> Delete(string roleName)
        {

            return View(await MultiTenantRoleDeleteConfirmDto.FormRoleDeleteConfirmDtoAsync(roleName, _authRolesAdmin));
        }

        [HasPermission(Example4Permissions.RoleChange)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(RoleDeleteConfirmDto input)
        {
            var status = await _authRolesAdmin.DeleteRoleAsync(input.RoleName, input.ConfirmDelete?.Trim() == input.RoleName);
                
            if (status.HasErrors)
                return RedirectToAction(nameof(ErrorDisplay),
                    new { errorMessage = status.GetAllErrors() });

            return RedirectToAction(nameof(Index), new { message = status.Message });
        }
    }
}
