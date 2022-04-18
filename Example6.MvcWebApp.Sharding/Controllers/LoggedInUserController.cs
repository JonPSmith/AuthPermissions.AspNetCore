// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc;

namespace Example6.MvcWebApp.Sharding.Controllers
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
                var userId = User.GetUserIdFromUser();
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
