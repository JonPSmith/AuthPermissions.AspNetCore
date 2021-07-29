using Microsoft.AspNetCore.Mvc;
using AuthPermissions.AdminCode;
using AuthPermissions.PermissionsCode;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class RolesController : Controller
    {
        private readonly IAuthRolesAdminService _authRolesAdmin;

        public RolesController(IAuthRolesAdminService authRolesAdmin)
        {
            _authRolesAdmin = authRolesAdmin;
        }

        //[HasPermission(Example4Permissions.PermissionRead)]
        public IActionResult ListPermissions()
        {
            var permissionDisplay = _authRolesAdmin.GetPermissionDisplay(false);

            return View(permissionDisplay);
        }

        public IActionResult UserPermissions([FromServices] IUsersPermissionsService service)
        {
            return View(service.PermissionsFromUser(HttpContext.User));
        }
    }
}
