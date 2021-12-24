using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.AspNetCore.Mvc;

namespace Example5.MvcWebApp.AzureAdB2C.Controllers
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
    }
}
