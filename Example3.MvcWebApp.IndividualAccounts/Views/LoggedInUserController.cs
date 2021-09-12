using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;

namespace Example4.MvcWebApp.IndividualAccounts.Controllers
{
    public class LoggedInUserController : Controller
    {
        public IActionResult Index()
        {
            return View(User);
        }

        public async Task<IActionResult> AuthUserInfo([FromServices]IAuthUsersAdminService service)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.Claims.GetUserIdFromClaims();
                var status = await service.FindAuthUserByUserIdAsync(userId);

                if (status.HasErrors)
                    return RedirectToAction("ErrorDisplay", "AuthUsers",
                        new { errorMessage = status.GetAllErrors() });

                return View(AuthUserDisplay.DisplayUserInfo(status.Result));
            }
            return View((AuthUserDisplay)null);
        }

        public IActionResult UserPermissions([FromServices] IUsersPermissionsService service)
        {
            return View(service.PermissionsFromUser(HttpContext.User));
        }
    }
}
